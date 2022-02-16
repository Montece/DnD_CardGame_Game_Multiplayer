using Newtonsoft.Json;
using UnityEngine;

[System.Serializable]
public class Card
{
    [SerializeField]
    public string Guid; //���������� �����, ������ �������� ����������
    [SerializeField]
    public string ID; //����� ��� ��������� ���������� � �������� � �� �����������
    [SerializeField]
    public string PackID; //����� ��� ��������� ���������� � �������� � �� �����������
    [SerializeField]
    public BattleCard BattleCard; //�����, ������� ����� ���������

    [JsonIgnore]
    public InfoCard InfoCard
    {
        get
        {
            if (infoCard == null) LoadCardInfo();
            return infoCard;
        }
        private set
        {
            infoCard = value;
        }
    }

    [JsonIgnore]
    private InfoCard infoCard;

    private Card() {}

    public void ResetBattleCard()
    {
        BattleCard = InfoCard.BattleReference.Clone();
    }

    public void LoadCardInfo()
    {
        CardsPack pack = GameResources.Instance.GetCardsPack(PackID);
        if (pack == null) return;
        infoCard = pack.GetCardInfo(ID);
    }

    public static Card CreateCard(string id, string packID)
    {
        Card card = new Card()
        {
            Guid = System.Guid.NewGuid().ToString(),
            ID = id,
            PackID = packID,
        };

        card.ResetBattleCard();

        return card;
    }

    public bool IsBuffedCard<T>()
    {
        return Helper.IsType<T>(BattleCard.DefaultBuff);
    }

    public override bool Equals(object card)
    {
        if (card == null) return false;
        if (Helper.IsType<Card>(card)) return ((Card)card).Guid == Guid;
        return false;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}