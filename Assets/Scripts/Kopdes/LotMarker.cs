using UnityEngine;

public class LotMarker : MonoBehaviour
{
    public LotData lotData;
    public LotManager lotManager; // drag the MapRoot object in

    void OnMouseDown()
    {
        lotManager.ConfirmLotSelection(transform.position, lotData);
    }
}