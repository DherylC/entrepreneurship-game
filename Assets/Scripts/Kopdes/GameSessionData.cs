// A plain static class's fields persist for the lifetime of the app (until you quit
// Play mode or the built game), surviving scene loads. This is the standard beginner-friendly
// way to pass a small amount of data between two scenes without a full save system.
public static class GameSessionData
{
    public static LotSite ChosenLot;
    public static LotData ChosenLotData;
}