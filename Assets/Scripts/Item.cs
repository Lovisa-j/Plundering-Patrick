using UnityEngine;

[CreateAssetMenu(fileName = "new Item", menuName = "Items/new Item")]
public class Item : ScriptableObject
{
    public string itemName;
    public GameObject itemPrefab;
    public Vector3 itemPositionOffset;
    public Vector3 itemRotationOffset;
    public Vector3 itemScaleMultiplier;
    public int worth;
}
