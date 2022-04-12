namespace LilyHid
{
    internal enum QmkReceivedCommandId : byte
    {
        IncreaseLights = 0x01,
        DecreaseLights,
        Test = 0xF1,
    }
}
