using System.Collections.Generic;
using AttributesBox;
using UnityEngine;

namespace SpaceToyProject.Core
{
    public enum PlayerType { Player, Enemy }
    [System.Serializable]
    public class UserBaseData
    {
        [Header("공동 데이터")]
        [HorizontalLine]
        [SerializeField] int maxHeartCount;
        public int MaxHeartCount => maxHeartCount;
        [ReadOnly][SerializeField] int heartCount;
        public int HeartCount => heartCount;
        [SerializeField] int maxItemCount;
        [ReadOnly][SerializeField] List<Item> items;
        public List<Item> Items => items;
        protected UserBaseData()
        {
            items = new List<Item>();
        } 
        public void SetFullHeartCount() { heartCount = maxHeartCount; }
        public bool UpHeartCount() 
        { 
            if(heartCount < maxHeartCount) heartCount++; 
            else return false;
            return true;
        }
        public bool DownHeartCount() 
        { 
            // if(heartCount > 0) heartCount--; 
            // else return false;
            // return true;

            //1에서 깎는 요청이 왔을 때 false 반환
            if (heartCount > 1) heartCount--;
            else return false;
            return true;
        }
        public void SetItem(List<Item> items, bool reset = false)
        {
            if(reset) this.items.Clear();
            for(int i = 0; i < items.Count; i++)
                this.items.Add(items[i]);
            if(items.Count > maxItemCount) Debug.Log("입력받은 아이템 수가 한계치보다 많습니다");
        }
        public void UseItem(Item type)
        {
            for(int i = 0; i < items.Count; i++)
                if(items[i] == type) { items.RemoveAt(i); return; }
        }
    }
    [System.Serializable]
    public class PlayerData : UserBaseData {}
    [System.Serializable]
    public class EnemyData : UserBaseData
    {
        [Header("적 전용 데이터")]
        [HorizontalLine]
        [SerializeField] List<MemberType> expectList; //추측 목록
        public List<MemberType> ExpectList => expectList;
        public void ResetExpectList(int memberCount)
        {
            expectList.Clear();
            for(int i = 0; i < memberCount; i++)
                expectList.Add(MemberType.Unknown);
        }
        public void EditExpectList(int index, MemberType type)
        {
            expectList[index] = type;
        }
        public MemberType GetNowExpectMember()
        {
            if(expectList[CoreManager.Instance.NowTurn] == MemberType.Unknown)
            {
                float unknownAliveRate = CalculateUnknownAliveRate();
                if(unknownAliveRate == 1) EditExpectList(CoreManager.Instance.NowTurn, MemberType.Alive);
                if(unknownAliveRate == 0) EditExpectList(CoreManager.Instance.NowTurn, MemberType.Dead);
            }
            return expectList[CoreManager.Instance.NowTurn];
        }
        /// <summary>
        /// 언노운 중 생존자 비율을 계산합니다(언노운이 0이면 float.infinity 반환)
        /// </summary>
        /// <returns></returns>
        public float CalculateUnknownAliveRate()
        {
            MoonData moonData = CoreManager.Instance.MoonData;
            int realAlive = moonData.SelectedAliveMember;
            int realDead = moonData.SelectedDeadMember;
            //각 비율
            int unknown = 0, exAlive = 0, exDead = 0;
            for(int i = 0; i < moonData.SelectedMember; i++)
            {
                switch(expectList[i])
                {
                    case MemberType.Unknown: unknown++; break;
                    case MemberType.Dead: exDead++; break;
                    case MemberType.Alive: exAlive++; break;
                }
            }
            //실제에서 예측 빼기
            float cAlive = realAlive - exAlive;
            float cDead = realDead - exDead;
            //cAlive + cDead = unknown
            return cAlive/unknown;
        }
        public MemberType CalculateBestInThisTurn()
        {
            if(expectList[CoreManager.Instance.NowTurn] != MemberType.Unknown) 
                return expectList[CoreManager.Instance.NowTurn];
            float cRate = CalculateUnknownAliveRate();   
            float cResult = Random.Range(0f, 1f);
            if(cResult < cRate) return MemberType.Alive;
            else return MemberType.Dead;
        }
    }
    public class CoreManager : MonoBehaviour
    {
        #region  ### Core ###
        public static CoreManager Instance { get; private set;}
        [ReadOnly][SerializeField] GameObject instanceTarget; //debug
        void InitManager()
        {
            if(Instance != null)
            {
                Destroy(this.gameObject);
                return;
            }

            Instance = this;
            instanceTarget = this.gameObject;
        }
        #endregion

        [HorizontalLine]
        [ReadOnly][SerializeField] bool ReadyToStart; //인스펙터 확인용
        public System.Action ReadyToStartAlert {get; set;}

        [HorizontalLine]
        [SerializeField] int nowTurn; //0 ~ 7
        public int NowTurn => nowTurn;
        public System.Action EndTurnAlert {get; set;}
        public void ResetTurn() { nowTurn = 0; }
        public bool CheckTurnAndAddCount()
        {
            //마지막 턴을 종료하고 새 턴 검사할 때(마지막 턴의 +1 상태일 때)
            if (nowTurn >= moonData.Members.Count - 1)
            {
                EndTurnAlert?.Invoke();
                return false;
            }
            else nowTurn++;
            return true;
        }
        [HorizontalLine]
        [SerializeField] MoonData moonData;
        public MoonData MoonData => moonData;
        [SerializeField] PlayerData playerData;
        public PlayerData PlayerData => playerData;
        [SerializeField] EnemyData enemyData;
        public EnemyData EnemyData => enemyData;

        void Awake()
        {
            InitManager();
        }

        public void ResetData()
        {
            playerData.SetFullHeartCount();
            playerData.Items.Clear();
            enemyData.SetFullHeartCount();
            enemyData.Items.Clear();
            ResetTurn();
            moonData = null;
        }
        public void SetNewTurn(MoonData moonData, List<Item> playerItem, List<Item> enemyItem)
        {
            ResetTurn();
            this.moonData = moonData;
            enemyData.ResetExpectList(moonData.SelectedMember);
            playerData.SetItem(playerItem, true);
            enemyData.SetItem(enemyItem, true);
        }
    }
}
