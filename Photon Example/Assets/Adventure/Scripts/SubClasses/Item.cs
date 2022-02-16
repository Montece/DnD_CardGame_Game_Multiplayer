using Newtonsoft.Json;
using UnityEngine;

/// <summary> ������ ������� ��������. ����� ������ �� ����� ��� �������� � ���� ID � InfoItem </summary>
[System.Serializable]
public class Item
{
    [SerializeField]
    public string Guid; //���������� �����, ������ ������� ����������
    [SerializeField]
    public string ID; //����� ��� ��������� ���������� � �������� � ��� �����������

    [JsonIgnore]
    public InfoItem InfoItem
    {
        get
        {
            if (infoItem == null) LoadItemInfo();
            return infoItem;
        }
        private set
        {
            infoItem = value;
        }
    }

    [JsonIgnore]
    private InfoItem infoItem;

    private Item() {}

    private void LoadItemInfo()
    {
        infoItem = GameResources.Instance.GetItemInfo(ID);
    }

    public static Item CreateItem(string id)
    {
        Item item = new Item()
        {
            Guid = System.Guid.NewGuid().ToString(),
            ID = id
        };

        return item;
    }

    public override bool Equals(object item)
    {
        if (item == null) return false;
        if (Helper.IsType<Item>(item)) return ((Item)item).Guid == Guid;
        return false;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}
