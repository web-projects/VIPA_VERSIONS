using Common.Helpers;

namespace Common.Execution
{
    public class Modes
    {
        public enum Execution
        {
            [StringValue("Undefined")]
            Undefined,
            [StringValue("Console")]
            Console,
            [StringValue("Standalone")]
            StandAlone,
        }
    }
}
