public class DTXModel
{
    public DTXHeaderModel Header { get; set; }
    public string RelativePathToDTX { get; set; }
    public byte[] Data { get; set; }
}