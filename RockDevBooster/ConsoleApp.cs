using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Utility class to help with launching console apps with full input and output
/// redirection.
/// </summary>
public class ConsoleApp
{
    #region Private Fields

    /// <summary>
    /// The process that we are executing.
    /// </summary>
    private readonly Process process = new Process();

    /// <summary>
    /// Used to coordinate writes from multiple thread sources.
    /// </summary>
    private readonly object theLock = new object();

    /// <summary>
    /// The context to call the input handlers from.
    /// </summary>
    private SynchronizationContext context;

    /// <summary>
    /// Stores data to be written on the background write thread.
    /// </summary>
    private string pendingWriteData;

    #endregion

    #region Public Properties

    /// <summary>
    /// Called when text on the stderr is detected.
    /// </summary>
    public event EventHandler<string> ErrorTextReceived;

    /// <summary>
    /// Called when the process has finished executing.
    /// </summary>
    public event EventHandler ProcessExited;

    /// <summary>
    /// Called when text on the stdout is detected.
    /// </summary>
    public event EventHandler<string> StandardTextReceived;

    /// <summary>
    /// The exit code of the process after it has finished.
    /// </summary>
    public int ExitCode
    {
        get { return this.process.ExitCode; }
    }

    /// <summary>
    /// Checks if the process is still running or not.
    /// </summary>
    public bool Running { get; private set; }

    /// <summary>
    /// The working directory to execute the application in.
    /// </summary>
    public string WorkingDirectory
    {
        get
        {
            return this.process.StartInfo.WorkingDirectory;
        }
        set
        {
            this.process.StartInfo.WorkingDirectory = value;
        }
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Initialize a new instance with the given executable name.
    /// </summary>
    /// <param name="appName">The application to be executed.</param>
    public ConsoleApp( string appName )
    {
        this.process.StartInfo.FileName = appName;
        this.process.StartInfo.RedirectStandardError = true;
        this.process.StartInfo.RedirectStandardInput = true;
        this.process.StartInfo.RedirectStandardOutput = true;
        this.process.EnableRaisingEvents = true;
        this.process.StartInfo.CreateNoWindow = true;
        this.process.StartInfo.UseShellExecute = false;
        this.process.StartInfo.StandardErrorEncoding = Encoding.UTF8;
        this.process.StartInfo.StandardOutputEncoding = Encoding.UTF8;

        this.process.Exited += this.ProcessOnExited;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Launch the executable with the given arguments. This method returns without
    /// waiting for the process to finish.
    /// </summary>
    /// <param name="args">The arguments to pass to the process.</param>
    public void ExecuteAsync( params string[] args )
    {
        if ( this.Running )
        {
            throw new InvalidOperationException( "Process is still Running. Please wait for the process to complete." );
        }

        this.process.StartInfo.Arguments = string.Join( " ", args.Select( s => s.Contains( " " ) ? "\"" + s + "\"" : s ) );
        this.context = SynchronizationContext.Current;

        this.process.Start();
        this.Running = true;

        //
        // Start a few background tasks to monitor the I/O streams of the process.
        //
        new Task( this.ReadOutputAsync ).Start();
        new Task( this.WriteInputTask ).Start();
        new Task( this.ReadOutputErrorAsync ).Start();
    }

    /// <summary>
    /// Terminate the process.
    /// </summary>
    public void Kill()
    {
        this.process.Kill();
    }

    /// <summary>
    /// Write the text to the stdin stream of the running process.
    /// </summary>
    /// <param name="data">The text to be written.</param>
    public void Write( string data )
    {
        if ( data == null )
        {
            return;
        }

        lock ( this.theLock )
        {
            this.pendingWriteData = data;
        }
    }

    /// <summary>
    /// Write the text as a complete line, appends the NewLine character.
    /// </summary>
    /// <param name="data">The text to be written to the stdin stream.</param>
    public void WriteLine( string data )
    {
        this.Write( data + Environment.NewLine );
    }

    #endregion

    #region Protected Methods

    /// <summary>
    /// Called when the process has exited. Default implementation calls the
    /// ProcessExited event handler.
    /// </summary>
    protected virtual void OnProcessExited()
    {
        this.ProcessExited?.Invoke( this, EventArgs.Empty );
    }

    /// <summary>
    /// Returns a subset of the given character array.
    /// </summary>
    /// <param name="input">The character array to use as the source.</param>
    /// <param name="startIndex">The starting index in the array.</param>
    /// <param name="length">The number of items to retrieve.</param>
    /// <returns>A character array that represents the subset of the input array.</returns>
    private static char[] SubArray( char[] input, int startIndex, int length )
    {
        List<char> result = new List<char>();
        for ( int i = startIndex; i < length; i++ )
        {
            result.Add( input[i] );
        }

        return result.ToArray();
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handles text coming in on the error stream of the process.
    /// </summary>
    /// <param name="text">The text that was received.</param>
    protected virtual void OnErrorTextReceived( string text )
    {
        EventHandler<string> handler = this.ErrorTextReceived;

        if ( handler != null )
        {
            if ( this.context != null )
            {
                this.context.Post( delegate { handler( this, text ); }, null );
            }
            else
            {
                handler( this, text );
            }
        }
    }

    /// <summary>
    /// Handles text coming in on the stdin stream of the process.
    /// </summary>
    /// <param name="text">The text that was received on the stream.</param>
    protected virtual void OnStandardTextReceived( string text )
    {
        EventHandler<string> handler = this.StandardTextReceived;

        if ( handler != null )
        {
            if ( this.context != null )
            {
                this.context.Post( delegate { handler( this, text ); }, null );
            }
            else
            {
                handler( this, text );
            }
        }
    }

    /// <summary>
    /// The process has exited. Call our internal method to allow subclasses to
    /// override the default functionality.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="eventArgs"></param>
    private void ProcessOnExited( object sender, EventArgs eventArgs )
    {
        this.OnProcessExited();
    }

    #endregion

    #region Tasks

    /// <summary>
    /// Processes the stdin stream of the executing application.
    /// </summary>
    private async void ReadOutputAsync()
    {
        var sb = new StringBuilder();
        var buff = new char[1024];
        int length;

        while ( this.process.HasExited == false )
        {
            sb.Clear();

            length = await this.process.StandardOutput.ReadAsync( buff, 0, buff.Length );
            sb.Append( SubArray( buff, 0, length ) );
            this.OnStandardTextReceived( sb.ToString() );
            Thread.Sleep( 1 );
        }

        this.Running = false;
    }

    /// <summary>
    /// Processes the stdout stream of the executing application.
    /// </summary>
    private async void ReadOutputErrorAsync()
    {
        var sb = new StringBuilder();

        do
        {
            sb.Clear();
            var buff = new char[1024];
            int length = await this.process.StandardError.ReadAsync( buff, 0, buff.Length );
            sb.Append( SubArray( buff, 0, length ) );
            this.OnErrorTextReceived( sb.ToString() );
            Thread.Sleep( 1 );
        }
        while ( this.process.HasExited == false );
    }

    /// <summary>
    /// Processes the stdout stream of the executing application.
    /// </summary>
    private async void WriteInputTask()
    {
        while ( this.process.HasExited == false )
        {
            Thread.Sleep( 1 );

            if ( this.pendingWriteData != null )
            {
                await this.process.StandardInput.WriteLineAsync( this.pendingWriteData );
                await this.process.StandardInput.FlushAsync();

                lock ( this.theLock )
                {
                    this.pendingWriteData = null;
                }
            }
        }
    }

    #endregion
}