namespace RateLimit.Interfaces.Dtos;
public class IsolationTestResultDto
{
    public string IsolationLevel { get; set; }
    public string NameBefore { get; set; }
    public string NameAfter { get; set; }
    public bool Changed { get; set; }
}