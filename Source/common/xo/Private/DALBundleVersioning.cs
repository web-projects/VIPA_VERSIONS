namespace Common.XO.Private
{
    /// <summary>
    /// schema(7):
    /// {sig}.{app}.{type}.{terminal_type}.{front_end}.{entity}.{model}.{version}.{date_code}
    /// 
    /// example:
    ///             firmware   : "sphere.sphere.vipa....p200.6_2_8_11.210517"
    ///                        : "sphere.njt.vipa....p200.6_2_8_11.210517"
    ///             
    ///             emv config : "sphere.sphere.emv.attended.FD...6_2_8_11.210517"
    ///                        : "sphere.sphere.emv.unattended.FD...6_2_8_11.210517"
    ///             
    ///             idle screen: "sphere.sphere.idle...199.p200..210517"
    ///
    /// </summary>
    public class DALBundleVersioning
    {
        public string Signature { get; set; }
        public string Application { get; set; }
        public string Type { get; set; }
        public string TerminalType { get; set; }
        public string FrontEnd { get; set; }
        public string Entity { get; set; }
        public string Model { get; set; }
        public string Version { get; set; }
        public string DateCode { get; set; }
    }

    public enum VerifoneSchemaIndex : int
	{
        Sig = 0,
        App = 1,
        Type = 2,
        TerminalType = 3,
        FrontEnd = 4,
        Entity = 5,
        Model = 6,
        Version = 7,
        DateCode = 8
    }
}
