# RockLauncher

<img src="https://github.com/cabal95/RockLauncher/raw/master/RockLauncher/resources/RockLauncher.svg" title="Rock Launcher" alt="Rock Launcher" width="200px" style="float: right;">

Provides a GUI application that allows you to download any version of Rock from github,
compile it and store it as a template. These templates can be deployed any number of
times and launched to give you a "known good state" whenever you are testing code or
functionality.

All data is stored in your `User\AppData\Local\RockLauncher` directory.

Requires Visual Studio, and SQL Local DB (should be installed with VS).

## Future Enhancement Ideas

* Convert instance to a Template for easy re-deployment in that exact state.

## GitHub Versions

This tab allows you to see all tagged versions on GitHub and import one as a new template.
When you import a version, it is downloaded, built and then the RockWeb folder is compressed
into a template file for deployment later. Each version only needs to be downloaded once as
the template can be deployed multiple times.

![GitHub Version](https://github.com/cabal95/RockLauncher/raw/master/Documentation/GitHubVersionsScreen.png)

> Note: Each download is larger than 500MB, so expect it to take some time. Once the template
> has been built the on-disk size is closer to 80MB as a template.

## Templates

This tab lets you see all temlates you have imported. You may delete them or deploy them.
A single template can be deployed multiple times, though you must give each instance a
unique name.

![Templates](https://github.com/cabal95/RockLauncher/raw/master/Documentation/TemplateScreen.png)

## Instances

Each instance represents a fully functional Rock install on disk. Currently you may only
start one instance at a time, but a future version may provide the ability to run multiple
instances concurrently. The Rock instance is configured to use a LocalDb database with the
database file stored in the App_Data directory. This keeps each instance isolated from your
normal SQL Server so you do not have to worry about poluting the database list.

Worry not, you can still use SQL Server Management Studio to connect to your Rock instance
database for troubleshooting and testing. To connect use the server name
`(LocalDb)\RockLauncher` with Windows Authentication.

Because these instances are completely self contained, you can manually copy the entire
instance directory to make a backup of the state and then later restore it. Though, make
sure you stop the instance and quit the RockLauncher application before doing so to ensure
that the database has been closed.

![Instances](https://github.com/cabal95/RockLauncher/raw/master/Documentation/InstancesScreen.png)

> Note: Because the SQL Database is stored in the instance's App_Data directory you can
> expect the instance size to be around 600MB. 200MB of that is the transaction log from
> initial Rock setup. A later version will include code to trim the log file each time
> the instance is stopped.