namespace SwagMatch.Core.Tools.MockGenerator;

public sealed class Config
{
    public int AppCnt { get; set; } = 5;
    public int PathPerAppCnt { get; set; } = 10;
    public int OppPerPathPerAppCnt { get; set; } = 1;
    public int ReqBodyOrParamCnt { get; set; } = 6;
}
