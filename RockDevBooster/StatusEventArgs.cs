namespace com.blueboxmoon.RockDevBooster
{
    public class StatusEventArgs
    {
        public Status Status { get; set; }

        public string Message { get; set; }
    }

    public enum Status
    {
        Success = 0,
        Failed = 1
    }
}
