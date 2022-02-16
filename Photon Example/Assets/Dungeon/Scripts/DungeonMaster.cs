using ExitGames.Client.Photon;
using Photon.Pun;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DungeonMaster : MonoBehaviour
{
    //����
    private CardsContainer Host_Arm;
    private CardsContainer Client_Arm; //����������������
    //����
    private CardsContainer Host_Field; //����������������
    private CardsContainer Client_Field; //����������������
    //������, �� ������� ��������� �����
    private CardsContainer Host_Deck;
    private CardsContainer Client_Deck; //����������������

    //���������� �����������
    private int Host_ManaCurrent;
    private int Client_ManaCurrent;
    //��������� �������� ����, ���������� ���������� �� ���� ����
    private int Host_ManaLocalMaximum;
    private int Client_ManaLocalMaximum;
    //�������� ����, ������� � �������� ����� ����
    private int Host_ManaMaximum;
    private int Client_ManaMaximum;
    //������� ��������
    private int Host_HealthCurrent;
    private int Client_HealthCurrent;
    //������������ ��������
    private int Host_HealthMaximum;
    private int Client_HealthMaximum;
    //����� �� ��������� � ����
    private bool Host_CanBeAttacked;
    private bool Client_CanBeAttacked;

    //��� ��� Entity 1 ��� Entity 2
    private bool Host_Turn;
    //����� ����
    private int Turn_Number = 1;
    //������� �������� ���� � ������
    private const int START_CARDS_COUNT = 2;
    //������������ ���������� ���� �� ����� � ������ ������������
    private const int MAX_FIELD_CARDS_COUNT = 6;
    //������������ ���������� ���� � ���� ����� � ������ ������������
    private const int MAX_ARM_CARDS_COUNT = 10;

    private int PlayerActorNumberAttacker;
    private int PlayerActorNumberDefender;
    private Player Host;
    private Player Client;

    private UiMovement movement;
    private AudioSource source;
    private PhotonView PhotonView;

    //Singleton
    public static DungeonMaster Instance;

    private void Awake()
    {
        //Singleton
        Instance = this;

        movement = GetComponent<UiMovement>();
        source = GetComponent<AudioSource>();
        PhotonView = GetComponent<PhotonView>();

        SceneManager.sceneLoaded += SceneLoaded;
    }

    [PunRPC]
    public void RpcStartDungeon(int playerActorNumberAttacker, int playerActorNumberDefender)
    {
        PlayerActorNumberAttacker = playerActorNumberAttacker;
        PlayerActorNumberDefender = playerActorNumberDefender;
        PhotonNetwork.LoadLevel("Dungeon");
    }

    private void SceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        if (scene.name == "Dungeon") StartDungeon();
    }

    ///<summary> ���� �������� ����� ��� ��������� DungeonInterface ������� </summary>
    private void StartDungeon()
    {
        Host = HostManager.Instance.CurrentPlayer;
        Client = HostManager.Instance.GetPlayer(Host.ActorNumber == PlayerActorNumberAttacker ? PlayerActorNumberDefender : PlayerActorNumberAttacker);

        //������������� ���� �������� ��������
        Host_Arm = new CardsContainer();
        Client_Arm = new CardsContainer();
        Host_Field = new CardsContainer();
        Client_Field = new CardsContainer();
        Host_Deck = new CardsContainer();
        Client_Deck = new CardsContainer();

        //�������� ������ �����������
        for (int i = 0; i < Host.Inventory.Cards.Count; i++)
            Host_Deck.Cards.Add(Host.Inventory.Cards[i]);
        for (int i = 0; i < Client.Inventory.Cards.Count; i++)
            Client_Deck.Cards.Add(Client.Inventory.Cards[i]);

        //����� ������ ���� ��� ��������
        for (int i = 0; i < Host_Deck.Cards.Count; i++) Host_Deck.Cards[i].ResetBattleCard();
        for (int i = 0; i < Client_Deck.Cards.Count; i++) Client_Deck.Cards[i].ResetBattleCard();

        //�������� ����������
        Turn_Number = 0;
        Host_ManaCurrent = 0;
        Client_ManaCurrent = 0;
        Host_ManaLocalMaximum = 0;
        Client_ManaLocalMaximum = 0;
        Host_ManaMaximum = Host.ManaMaximum;
        Client_ManaMaximum = Client.ManaMaximum;
        Host_HealthCurrent = Host.HealthCurrent;
        Client_HealthCurrent = Client.HealthCurrent;
        Host_HealthMaximum = Host.HealthMaximum;
        Client_HealthMaximum = Client.HealthMaximum;
        Host_CanBeAttacked = true;
        Client_CanBeAttacked = true;

        //������ ��������� ���� �� ����
        for (int i = 0; i < START_CARDS_COUNT; i++) GiveCardFromDeck(Host_Arm, Host_Deck);
        for (int i = 0; i < START_CARDS_COUNT; i++) GiveCardFromDeck(Client_Arm, Client_Deck);

        //�������������� ������ ������
        SyncClientArmCards();
        SyncHostFieldCards();
        SyncClientFieldCards();

        //���������� �������� � ���� ��� � �����
        RenderCards();

        //������ ����� ��� ��� ��������
        SetTurn(Host.ActorNumber == PlayerActorNumberAttacker);

        //���������� ���� �������
        RenderNicknames();

        //���������� ���� �������
        RenderHostMana();
        RenderClientMana();

        //���������� ����� �������
        RenderDefence();
    }

    ///<summary> ������ � ���� ����� �� ������ </summary>
    private void GiveCardFromDeck(CardsContainer arm, CardsContainer deck)
    {
        if (deck.Cards.Count == 0 || arm.Cards.Count >= MAX_ARM_CARDS_COUNT) return;
        else
        {
            Card card = deck.Cards[Random.Range(0, deck.Cards.Count)];
            arm.Cards.Add(card);
            deck.Cards.Remove(card);
        }
    }

    ///<summary> ����������� ���� ��� � ����� </summary>
    private void RenderCards()
    {
        PhotonView.RPC("RpcRenderCardsHost", RpcTarget.MasterClient);
        PhotonView.RPC("RpcRenderCardsClient", RpcTarget.Others, Host_Arm.Cards.Count);
    }

    ///<summary> ����������� ���������� ���� </summary>
    private void RenderHostMana()
    {
        DungeonInterface.Instance.ShowMana(Host_ManaCurrent, Host_ManaLocalMaximum);
    }

    ///<summary> ����������� ���������� ���� </summary>
    private void RenderClientMana()
    {
        PhotonView.RPC("RpcRenderManaClient", RpcTarget.Others, Client_ManaCurrent, Client_ManaLocalMaximum);
    }

    ///<summary> ����������� ���������� �������� </summary>
    private void RenderDefence()
    {
        PhotonView.RPC("RpcRenderDefences", RpcTarget.All, Host_HealthCurrent, Host_HealthMaximum, Client_HealthCurrent, Client_HealthMaximum);
    }

    ///<summary> ����������� �������� ������� </summary>
    private void RenderNicknames()
    {
        PhotonView.RPC("RpcRenderNicknames", RpcTarget.All, Host.Nickname, Client.Nickname);
    }

    [PunRPC]
    private void RpcRenderCardsHost()
    {
        DungeonManager.Instance.ShowMyArm(Host_Arm.Cards);
        DungeonManager.Instance.ShowEnemyArm(Client_Arm.Cards.Count);
        DungeonManager.Instance.ShowMyField(Host_Field.Cards);
        DungeonManager.Instance.ShowEnemyField(Client_Field.Cards);
    }

    ///<summary> ������ ��������, ��� ������ �����, host ��� !host </summary>
    private void SetTurn(bool host_turn)
    {
        Host_Turn = host_turn;
        Turn_Number++;

        if (Host_Turn)
        {
            //�������� ������ ����
            //����������� � ��������������� ����
            IncreaseHostMaxMana();
            RestoreHostMana();
            RenderHostMana();

            //������ ����� �� ����
            GiveCardFromDeck(Host_Arm, Host_Deck);

            //������������ ����� ��� ��������
            for (int i = 0; i < Host_Field.Cards.Count; i++) Host_Field.Cards[i].BattleCard.CanAttack = true;
        }
        else
        {
            //�������� ������ ������
            //����������� � ��������������� ����
            IncreaseClientMaxMana();
            RestoreClientMana();
            RenderClientMana();

            //������ ����� �� ����
            GiveCardFromDeck(Client_Arm, Client_Deck);
            SyncClientArmCards();

            //������������ ����� ��� ��������
            for (int i = 0; i < Client_Field.Cards.Count; i++) Client_Field.Cards[i].BattleCard.CanAttack = true;
            SyncClientFieldCards();
        }

        PhotonView.RPC("RpcSetTurn", RpcTarget.All, host_turn);

        RenderCards(); 
    }

    [PunRPC]
    public void RpcSwitchTurn(int calledActorNumber)
    {
        if (!IsMyTurn(calledActorNumber)) return;

        SetTurn(!Host_Turn);
    }

    [PunRPC]
    private void RpcMoveCardToField(int calledActorNumber, string card_guid)
    {
        if (!IsMyTurn(calledActorNumber)) return;

        if (Host_Turn)
        {
            if (Host_Field.Cards.Count >= MAX_FIELD_CARDS_COUNT) return;
            Card card = GetCard(Host_Arm.Cards, card_guid);

            if (Host_ManaCurrent >= card.BattleCard.ManaCost)
            {
                Host_ManaCurrent -= card.BattleCard.ManaCost;
                Host_Arm.Cards.Remove(card);
                card.BattleCard.CanAttack = card.IsBuffedCard<DashBuff>();
                Host_Field.Cards.Add(card);
                if (card.IsBuffedCard<AddCardBuff>())
                {
                    AddCardBuff buff = (AddCardBuff)card.BattleCard.DefaultBuff;
                    if (Host_Field.Cards.Count < MAX_FIELD_CARDS_COUNT)
                    {
                        Card card_new = Card.CreateCard(buff.CardID, buff.CardPackID);
                        card_new.BattleCard.CanAttack = card_new.IsBuffedCard<DashBuff>();
                        Host_Field.Cards.Add(card_new);
                    }
                }
                if (card.IsBuffedCard<MyStatsIncreaseAllBuff>())
                {
                    MyStatsIncreaseAllBuff buff = (MyStatsIncreaseAllBuff)card.BattleCard.DefaultBuff;
                    for (int i = 0; i < Host_Field.Cards.Count; i++)
                    {
                        if (Host_Field.Cards[i].Guid != card.Guid)
                        {
                            Host_Field.Cards[i].BattleCard.Attack += buff.Attack;
                            Host_Field.Cards[i].BattleCard.Defence += buff.Defence;
                        }
                    }
                }
                if (card.IsBuffedCard<EnemyStatsDecreaseAllBuff>())
                {
                    EnemyStatsDecreaseAllBuff buff = (EnemyStatsDecreaseAllBuff)card.BattleCard.DefaultBuff;
                    for (int i = 0; i < Client_Field.Cards.Count; i++)
                    {
                        Client_Field.Cards[i].BattleCard.Attack -= buff.Attack;
                        Client_Field.Cards[i].BattleCard.Defence -= buff.Defence;
                        CheckCardDefence(Client_Field.Cards, Client_Field.Cards[i]);
                    } 
                }
                if (card.IsBuffedCard<AddManaBuff>())
                {
                    AddManaBuff buff = (AddManaBuff)card.BattleCard.DefaultBuff;
                    Host_ManaCurrent += buff.Mana;
                    if (Host_ManaCurrent > Host_ManaLocalMaximum) Host_ManaCurrent = Host_ManaLocalMaximum;
                }

                RecalculateGuardianBuff(Host_Field.Cards);

                SyncHostFieldCards();
                SyncClientFieldCards();
                SyncClientArmCards();
                RenderCards();
                RenderClientMana();
            }
        }
        else
        {
            if (Client_Field.Cards.Count >= MAX_FIELD_CARDS_COUNT) return;
            Card card = GetCard(Client_Arm.Cards, card_guid);

            if (Client_ManaCurrent >= card.BattleCard.ManaCost)
            {
                Client_ManaCurrent -= card.BattleCard.ManaCost;
                Client_Arm.Cards.Remove(card);
                card.BattleCard.CanAttack = card.IsBuffedCard<DashBuff>();
                Client_Field.Cards.Add(card);
                if (card.IsBuffedCard<AddCardBuff>())
                {
                    AddCardBuff buff = (AddCardBuff)card.BattleCard.DefaultBuff;
                    if (Client_Field.Cards.Count < MAX_FIELD_CARDS_COUNT)
                    {
                        Card card_new = Card.CreateCard(buff.CardID, buff.CardPackID);
                        card_new.BattleCard.CanAttack = card_new.IsBuffedCard<DashBuff>();
                        Client_Field.Cards.Add(card_new);
                    }
                }
                if (card.IsBuffedCard<MyStatsIncreaseAllBuff>())
                {
                    MyStatsIncreaseAllBuff buff = (MyStatsIncreaseAllBuff)card.BattleCard.DefaultBuff;
                    for (int i = 0; i < Client_Field.Cards.Count; i++)
                    {
                        if (Client_Field.Cards[i].Guid != card.Guid)
                        {
                            Client_Field.Cards[i].BattleCard.Attack += buff.Attack;
                            Client_Field.Cards[i].BattleCard.Defence += buff.Defence;
                        }
                    }
                }
                if (card.IsBuffedCard<EnemyStatsDecreaseAllBuff>())
                {
                    EnemyStatsDecreaseAllBuff buff = (EnemyStatsDecreaseAllBuff)card.BattleCard.DefaultBuff;
                    for (int i = 0; i < Host_Field.Cards.Count; i++)
                    {
                        Host_Field.Cards[i].BattleCard.Attack -= buff.Attack;
                        Host_Field.Cards[i].BattleCard.Defence -= buff.Defence;
                        CheckCardDefence(Host_Field.Cards, Host_Field.Cards[i]);
                    }
                }
                if (card.IsBuffedCard<AddManaBuff>())
                {
                    AddManaBuff buff = (AddManaBuff)card.BattleCard.DefaultBuff;
                    Client_ManaCurrent += buff.Mana;
                    if (Client_ManaCurrent > Client_ManaLocalMaximum) Client_ManaCurrent = Client_ManaLocalMaximum;
                }

                RecalculateGuardianBuff(Host_Field.Cards);
                RecalculateGuardianBuff(Client_Field.Cards);

                SyncHostFieldCards();
                SyncClientFieldCards();
                SyncClientArmCards();
                RenderCards();
                RenderClientMana();
            }
        }
    }

    [PunRPC]
    private void RpcAttackCard(int calledActorNumber, string attackCard_guid, string defenceCard_guid)
    {
        if (!IsMyTurn(calledActorNumber)) return;

        if (Host_Turn)
        {
            Card attackCard = GetCard(Host_Field.Cards, attackCard_guid);
            Card defenceCard = GetCard(Client_Field.Cards, defenceCard_guid);

            if (attackCard.BattleCard.CanAttack && defenceCard.BattleCard.CanBeAttacked)
            {
                if (HasCard(Host_Field.Cards, attackCard) && HasCard(Client_Field.Cards, defenceCard))
                {
                    CardsBattle(attackCard, defenceCard);

                    CheckCardDefence(Host_Field.Cards, attackCard);
                    CheckCardDefence(Client_Field.Cards, defenceCard);
                    RecalculateGuardianBuff(Host_Field.Cards);
                    RecalculateGuardianBuff(Client_Field.Cards);

                    SyncHostFieldCards();
                    SyncClientFieldCards();
                    RenderCards();
                }
            }
        }
        else
        {
            Card attackCard = GetCard(Client_Field.Cards, attackCard_guid);
            Card defenceCard = GetCard(Host_Field.Cards, defenceCard_guid);

            if (attackCard.BattleCard.CanAttack && defenceCard.BattleCard.CanBeAttacked)
            {
                if (HasCard(Host_Field.Cards, defenceCard) && HasCard(Client_Field.Cards, attackCard))
                {
                    CardsBattle(attackCard, defenceCard);
                    CheckCardDefence(Client_Field.Cards, attackCard);
                    CheckCardDefence(Host_Field.Cards, defenceCard);

                    RecalculateGuardianBuff(Host_Field.Cards);
                    RecalculateGuardianBuff(Client_Field.Cards);

                    SyncHostFieldCards();
                    SyncClientFieldCards();
                    RenderCards();
                }
            }
        }
    }

    [PunRPC]
    private void RpcAttackPlayer(int calledActorNumber, string attackCard_guid)
    {
        if (!IsMyTurn(calledActorNumber)) return;

        if (Host_Turn)
        {
            if (!Client_CanBeAttacked) return;

            Card attackCard = GetCard(Host_Field.Cards, attackCard_guid);

            if (attackCard.BattleCard.CanAttack)
            {
                if (HasCard(Host_Field.Cards, attackCard))
                {
                    DamageOpponentPlayer(attackCard);
                    if (!CheckPlayerWin())
                    {
                        RenderDefence();
                        SyncHostFieldCards();
                        SyncClientFieldCards();
                        RenderCards();
                    }
                }
            }
        }
        else
        {
            if (!Host_CanBeAttacked) return;

            Card attackCard = GetCard(Client_Field.Cards, attackCard_guid);

            if (attackCard.BattleCard.CanAttack)
            {
                if (HasCard(Client_Field.Cards, attackCard))
                {
                    DamageOpponentPlayer(attackCard);
                    if (!CheckPlayerWin())
                    {
                        RenderDefence();
                        SyncHostFieldCards();
                        SyncClientFieldCards();
                        RenderCards();
                    }
                }
            }
        }
    }

    private void DamageOpponentPlayer(Card attackCard)
    {
        if (attackCard == null) return;

        /*if (attackCard.InfoCard.AttackSound != null)
        {
            source.clip = attackCard.InfoCard.AttackSound;
            source.Play();
        }*/

        if (Host_Turn) Client_HealthCurrent -= attackCard.BattleCard.Attack;
        else Host_HealthCurrent -= attackCard.BattleCard.Attack;

        attackCard.BattleCard.CanAttack = false;
    }

    private bool HasCard(List<Card> cards, Card card)
    {
        return cards.Where(c => c.Guid == card.Guid).Count() == 1;
    }

    private Card GetCard(List<Card> cards, string card_guid)
    {
        return cards.Where(c => c.Guid == card_guid).FirstOrDefault();
    }

    private void RecalculateGuardianBuff(List<Card> cards)
    {
        bool hasGuardian = false;
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i].IsBuffedCard<GuardianBuff>())
            {
                hasGuardian = true;
                break;
            }
        }

        if (hasGuardian)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i].IsBuffedCard<GuardianBuff>())
                {
                    cards[i].BattleCard.CanBeAttacked = true;
                }
                else
                {
                    cards[i].BattleCard.CanBeAttacked = false;
                }
            }

            if (cards == Host_Field.Cards) Host_CanBeAttacked = false;
            if (cards == Client_Field.Cards) Client_CanBeAttacked = false;
        }
        else
        {
            for (int i = 0; i < cards.Count; i++)
            {
                cards[i].BattleCard.CanBeAttacked = true;
            }

            if (cards == Host_Field.Cards) Host_CanBeAttacked = true;
            if (cards == Client_Field.Cards) Client_CanBeAttacked = true;
        }
    }

    ///<summary> �������� ������������ ����� ���� ����� �� ��������� </summary>
    private void IncreaseHostMaxMana()
    {
        if (Host_ManaLocalMaximum < Host_ManaMaximum) Host_ManaLocalMaximum++;
    }

    ///<summary> �������� ������������ ����� ���� ������� �� ��������� </summary>
    private void IncreaseClientMaxMana()
    {
        if (Client_ManaLocalMaximum < Client_ManaMaximum) Client_ManaLocalMaximum++;
    }

    ///<summary> ������������ ������� ���� ����� �� ��������� </summary>
    private void RestoreHostMana()
    {
        Host_ManaCurrent = Host_ManaLocalMaximum;
    }

    ///<summary> ������������ ������� ���� ������� �� ��������� </summary>
    private void RestoreClientMana()
    {
        Client_ManaCurrent = Client_ManaLocalMaximum;
    }

    ///<summary> �������� ���� ���� </summary>
    private void CardsBattle(Card attackCard, Card defendCard)
    {
        if (attackCard == null || defendCard == null) return;
        if (attackCard == defendCard) return;

        /*if (attackCard.InfoCard.AttackSound != null)
        {
            source.clip = attackCard.InfoCard.AttackSound;
            source.Play();
        }*/

        defendCard.BattleCard.Defence -= attackCard.BattleCard.Attack;
        attackCard.BattleCard.Defence -= defendCard.BattleCard.Attack;

        attackCard.BattleCard.CanAttack = false;
    }

    ///<summary> �������� �������� ���� � �� ����������� </summary>
    private void CheckCardDefence(List<Card> field, Card card)
    {
        if (card.BattleCard.Defence <= 0)
        {
            /*if (card.InfoCard.DeathSound != null)
            {
                source.clip = card.InfoCard.DeathSound;
                source.Play();
            }*/

            field.Remove(card);
        }
    }

    ///<summary> �������� �������� ������� </summary>
    private bool CheckPlayerWin()
    {
        if (Host_HealthCurrent > Host_HealthMaximum) Host_HealthCurrent = Host_HealthMaximum;
        if (Host_HealthCurrent <= 0) Host_HealthCurrent = 0;
        if (Host_HealthCurrent <= 0)
        {
            WinClient();
            return true;
        }

        if (Client_HealthCurrent > Client_HealthMaximum) Client_HealthCurrent = Client_HealthMaximum;
        if (Client_HealthCurrent <= 0) Client_HealthCurrent = 0;
        if (Client_HealthCurrent <= 0)
        {
            WinHost();
            return true;
        }

        return false;
    }

    ///<summary> ��������, �� ����� �� ��� ��������� � ��������� �������� �� � ���� ��� </summary>
    private bool IsMyTurn(int actorNumber)
    {
        return Host_Turn && Host.ActorNumber == actorNumber || !Host_Turn && Client.ActorNumber == actorNumber;
    }

    private void WinHost()
    {
        Host.Inventory.AddItem(Item.CreateItem("i_player_trophy"));
        Host.Inventory.AddItems(Client.Inventory.GetItems());
        Client.Inventory.ClearItems();
        Host.Inventory.AddCards(Client.Inventory.GetCards());
        Client.Inventory.ClearCards();

        Host.HealthCurrent = Host_HealthCurrent;
        Client.HealthCurrent = Client_HealthCurrent;

        PhotonNetwork.LoadLevel("Adventure");
    }

    private void WinClient()
    {
        Client.Inventory.AddItem(Item.CreateItem("i_player_trophy"));
        Client.Inventory.AddItems(Host.Inventory.GetItems());
        Host.Inventory.ClearItems();
        Client.Inventory.AddCards(Host.Inventory.GetCards());
        Host.Inventory.ClearCards();

        Host.HealthCurrent = Host_HealthCurrent;
        Client.HealthCurrent = Client_HealthCurrent;

        PhotonNetwork.LoadLevel("Adventure");
    }

    ///<summary> ���������������� ���� ������� </summary>
    private void SyncClientArmCards()
    {
        Hashtable hash = new Hashtable { ["client_arm"] = Client_Arm };
        PhotonNetwork.CurrentRoom.SetCustomProperties(hash);
    }

    ///<summary> ���������������� ���� ������� </summary>
    private void SyncClientFieldCards()
    {
        Hashtable hash = new Hashtable { ["client_field"] = Client_Field };
        PhotonNetwork.CurrentRoom.SetCustomProperties(hash);
    }

    ///<summary> ���������������� ���� ����� </summary>
    private void SyncHostFieldCards()
    {
        Hashtable hash = new Hashtable { ["host_field"] = Host_Field };
        PhotonNetwork.CurrentRoom.SetCustomProperties(hash);
    }
}
