using UnityEngine;
using AttributesBox;
using System.Collections.Generic;
using SpaceToyProject.UI;
using SpaceToyProject.Anim;
using System.Collections;

namespace SpaceToyProject.Core
{
    public class GameManager : MonoBehaviour
    {
        [Header("입력 값")]
        [HorizontalLine]
        [Tooltip("0으로 갈 수록 적이 자주 걸리고 1로 갈 수록 플레이어가 자주 걸립니다")]
        [SerializeField] [Range(0f, 1f)] float turnOrderRate;
        [Header("Rule Generator")]
        [HorizontalLine]
        [SerializeField] MoonMemberGenerator moonMemberGenerator;
        [SerializeField] ItemListGenerator itemListGenerator;
        [Header("Result UI")]
        [HorizontalLine]
        [SerializeField] GameObject GameWinUIRef;
        [SerializeField] GameObject GameOverUIRef;
        [Header("Popup UI")]
        [HorizontalLine]
        [SerializeField] GameObject nextUIRef;
        [SerializeField] SelectionUI selectionItemUIRef;
        [SerializeField] GameObject yourTurnUIRef;
        [SerializeField] GameObject enemyTurnUIRef;
        [Header("State UI")]
        [HorizontalLine]
        [SerializeField] HeartUI playerHeartUIRef;
        [SerializeField] ItemUI playerItemUIRef;
        [HorizontalLine]
        [SerializeField] HeartUI enemyHeartUIRef;
        [SerializeField] ItemUI enemyItemUIRef;
        [SerializeField] TurnUI turnUIRef;
        [Header("Action")]
        [HorizontalLine]
        [SerializeField] MoonAnimController moonAnimRef;

        CoreManager coreRef;
        WaitForSeconds uiFrontDelay;
        WaitForSeconds uiMiddleDelay;
        WaitForSeconds nextUIDelay; //next ui의 timer와 시간 맞추기

        void Awake()
        {
            coreRef = CoreManager.Instance;
            coreRef.ReadyToStartAlert = StartGame;
            uiFrontDelay = new WaitForSeconds(0.5f);
            uiMiddleDelay = new WaitForSeconds(1.5f);
            nextUIDelay = new WaitForSeconds(3.5f);
        }

        /// <summary>
        /// 게임 시스템 시작
        /// </summary>
        public void StartGame()
        {
            SetMainData();
            Loop();
        }
        void SetMainData()
        {
            //data reset
            coreRef.ResetData();
            //ui reset
            playerHeartUIRef.ResetFullHeart();
            enemyHeartUIRef.ResetFullHeart();
            playerItemUIRef.ResetItem();
            enemyItemUIRef.ResetItem();
        }

        void Loop()
        {
            SetTurnData();
            StartCoroutine(SetPreTurnUI());
        }
        void SetTurnData()
        {
            MoonData moonData = moonMemberGenerator.GetMoonMembers();
            List<Item> playerItemData = itemListGenerator.GetItemList();
            List<Item> enemyItemData = itemListGenerator.GetItemList();
            coreRef.SetNewTurn(moonData, playerItemData, enemyItemData);

            playerItemUIRef.SetItemList(coreRef.PlayerData.Items);
            enemyItemUIRef.SetItemList(coreRef.EnemyData.Items);

            turnUIRef.SetNowTurn(coreRef.NowTurn+1);
            turnUIRef.SetEndTurn(moonData.SelectedMember);
        }
        IEnumerator SetPreTurnUI()
        {
            //next ui
            yield return StartCoroutine(SetNextUI());

            if(CheckOrder() == PlayerType.Player) 
            {
                yield return StartCoroutine(SetYourTurnUI());
                StartPlayerTurn();
            }
            else 
            {
                yield return StartCoroutine(SetEnemyTurnUI());
                StartEnemyTurn();
            }
        }
        IEnumerator SetTurnUI(PlayerType type)
        {
            turnUIRef.SetNowTurn(coreRef.NowTurn+1);
            if (type == PlayerType.Player) 
            {
                yield return StartCoroutine(SetYourTurnUI());
                StartPlayerTurn();
                yield break;
            }
            if (type == PlayerType.Enemy)
            {
                yield return StartCoroutine(SetEnemyTurnUI());
                StartEnemyTurn();
                yield break;
            }
        }
        IEnumerator SetNextUI()
        {
            yield return uiFrontDelay;
            nextUIRef.SetActive(true); //키면 자동 생신
            yield return nextUIDelay;
            nextUIRef.SetActive(false);
        }

        PlayerType CheckOrder()
        {
            float result = Random.Range(0, 1f);
            if(result < turnOrderRate) return PlayerType.Player;
            else return PlayerType.Enemy;
        }
        IEnumerator SetYourTurnUI()
        {
            yield return uiFrontDelay;
            yourTurnUIRef.SetActive(true);
            yield return uiMiddleDelay;
            yourTurnUIRef.SetActive(false);
        }
        IEnumerator SetEnemyTurnUI()
        {
            yield return uiFrontDelay;
            enemyTurnUIRef.SetActive(true);
            yield return uiMiddleDelay;
            enemyTurnUIRef.SetActive(false);
        }

        void StartPlayerTurn()
        {
            selectionItemUIRef.gameObject.SetActive(true);
        }
        void StartEnemyTurn()
        {
            EnemyData eData = coreRef.EnemyData;
            MemberType nowExpectMember = eData.GetNowExpectMember();
            float unknownAliveRate = eData.CalculateUnknownAliveRate();
            //하나의 선택지를 고를 때까지 생각하고 여기서 액션 실행 후 다시 생각 시작
            int itemBoxCount = 0, itemBlackholeCount = 0, itemMailCount = 0, itemPoisonCount = 0;
            for(int i = 0; i< eData.Items.Count; i++)
            {
                if(eData.Items[i] == Item.Box) itemBoxCount++;
                else if(eData.Items[i] == Item.BlackHole) itemBlackholeCount++;
                else if(eData.Items[i] == Item.Mail) itemMailCount++;
                else if(eData.Items[i] == Item.Poison) itemPoisonCount++;
            }

            //선택지
            //1순위. Box: 하트가 최대치가 아닐 때
            if (itemBoxCount > 0 && eData.HeartCount < eData.MaxHeartCount) 
            {
                Debug.Log("1순위 선택 - 하트가 최대치가 아닐 때");
                UseBoxBA();
                return;
            }
            //2순위. Come, Send: 마지막 턴일 때 현재 맴버에 따라 Come, Send 결정
            if (coreRef.NowTurn >= (coreRef.MoonData.SelectedMember - 1))
            {
                Debug.Log("2순위 선택 - Come, Send: 마지막 턴일 때 현재 맴버에 따라 Come, Send 결정");
                if(eData.CalculateBestInThisTurn() == MemberType.Alive) ComeBA();
                else SendBA();
                return;
            }
            //3순위. Mail: 앞으로 하나 이상 생존자가 있다고 판단될 때(중복 방지를 위해 체크한 부분 unknown 검사)
            if (itemMailCount > 0 && nowExpectMember == MemberType.Unknown && unknownAliveRate > 0 && eData.ExpectList[coreRef.MoonData.GetFirstAliveOne() + coreRef.NowTurn] == MemberType.Unknown) 
            {
                Debug.Log("3순위 선택 - Mail: 앞으로 하나 이상 생존자가 있다고 판단될 때(중복 방지를 위해 체크한 부분 unknown 검사)");
                UseMailBA();
                return;
            }
            //4순위. Blackhole: 현재 턴의 추측 맴버가 죽은 자거나 30%의 생존자 확률일 때
            if (itemBlackholeCount > 0 && (nowExpectMember == MemberType.Dead || (nowExpectMember == MemberType.Unknown && unknownAliveRate < 0.3f))) 
            {
                Debug.Log("4순위 선택 - Blackhole: 현재 턴의 추측 맴버가 죽은 자거나 30%의 생존자 확률일 때");
                UseBlackholeBA();
                return;
            }
            //5순위. Poison: 현재 턴의 추측 맴버가 죽은 자거나 죽은 자만 남았다 판단될 때
            if (itemPoisonCount > 0 && (nowExpectMember == MemberType.Dead)) //|| (nowExpectMember == MemberType.Unknown && unknownAliveRate == 0)
            {
                Debug.Log("5순위 선택 - Poison: 현재 턴의 추측 맴버가 죽은 자거나 죽은 자만 남았다 판단될 때");
                UsePoisonBA();
                return;
            }
            //6순위. Come: 현재 턴의 추측 맴버가 생존자거나 생존자만 남았다 판단될 때
            if(nowExpectMember == MemberType.Alive) //|| (nowExpectMember == MemberType.Unknown && unknownAliveRate == 1)
            {
                Debug.Log("6순위 선택 - Come: 현재 턴의 추측 맴버가 생존자거나 생존자만 남았다 판단될 때");
                ComeBA();
                return;
            }
            //7순위. Send: 현재 턴의 추측 맴버가 죽은 자거나 30%의 생존자 확률일 때
            if (nowExpectMember == MemberType.Dead || (nowExpectMember == MemberType.Unknown && unknownAliveRate < 0.3f))
            {
                Debug.Log("7순위 선택 - Send: 현재 턴의 추측 맴버가 죽은 자거나 30%의 생존자 확률일 때");
                SendBA();
                return;
            }
            //8순위. 그 외
            if(nowExpectMember == MemberType.Unknown)
            {
                Debug.Log("8순위 선택 - Come, Send 랜덤 결정");
                if(eData.CalculateBestInThisTurn() == MemberType.Alive) ComeBA();
                else SendBA();
                return;
            }
            //예외
            Debug.LogException(new System.Exception("적의 행동이 선택되지 않고 종료되었습니다"));
        }
        
        #region ### 플레이어 액션 ###
        public void UseBox()
        {
            //맥스 하트 경고
            if(!coreRef.PlayerData.UpHeartCount())
            {
                selectionItemUIRef.ViewHeartAttention();
                return;
            }
            coreRef.PlayerData.UseItem(Item.Box);

            selectionItemUIRef.gameObject.SetActive(false);
            moonAnimRef.Play(AnimationType.Box, PlayerType.Player, () =>
            {
                playerItemUIRef.UseItem(Item.Box);
                playerHeartUIRef.UpOneHeart();
                StartPlayerTurn();
            });
        }
        public void UseMail()
        {
            coreRef.PlayerData.UseItem(Item.Mail);

            selectionItemUIRef.gameObject.SetActive(false);
            moonAnimRef.Play(AnimationType.Mail, PlayerType.Player, () =>
            {
                playerItemUIRef.UseItem(Item.Mail);
                StartPlayerTurn();
            }); 
        }
        public void UseBlackhole()
        {
            MemberType target = coreRef.MoonData.Members[coreRef.NowTurn];
            coreRef.PlayerData.UseItem(Item.BlackHole);
            coreRef.EnemyData.EditExpectList(coreRef.NowTurn, target);

            selectionItemUIRef.gameObject.SetActive(false);
            moonAnimRef.Play(AnimationType.BlackHole, PlayerType.Player, target, () =>
            {
                playerItemUIRef.UseItem(Item.BlackHole);
                if(coreRef.CheckTurnAndAddCount()) { turnUIRef.SetNowTurn(coreRef.NowTurn+1); StartPlayerTurn(); }
                else Loop(); //턴 종료되면 턴 생성부터 다시 반복
            });
        }
        public void UsePoison()
        {
            MemberType target = coreRef.MoonData.Members[coreRef.NowTurn];
            if(target == MemberType.Alive) target = MemberType.Dead;
            else target = MemberType.Alive;
            coreRef.MoonData.Members[coreRef.NowTurn] = target;
            coreRef.PlayerData.UseItem(Item.Poison);

            selectionItemUIRef.gameObject.SetActive(false);
            moonAnimRef.Play(AnimationType.Poison, PlayerType.Player, () =>
            {
                playerItemUIRef.UseItem(Item.Poison);
                StartPlayerTurn();
            });
        }
        public void Send()
        {
            MemberType target = coreRef.MoonData.Members[coreRef.NowTurn];
            bool gameEnd = false;
            if(target == MemberType.Dead && !coreRef.EnemyData.DownHeartCount()) gameEnd = true;
            coreRef.EnemyData.EditExpectList(coreRef.NowTurn, target);
            
            selectionItemUIRef.gameObject.SetActive(false);
            moonAnimRef.Play(AnimationType.SendToE, PlayerType.Player, target, () =>
            {
                if(target == MemberType.Dead)
                {
                    enemyHeartUIRef.DownOneHeart();
                    if(gameEnd) 
                    {
                        GameWinUIRef.SetActive(true); 
                        return; 
                    }
                }

                if(coreRef.CheckTurnAndAddCount()) StartCoroutine(SetTurnUI(PlayerType.Enemy));
                else Loop(); //턴 종료되면 턴 생성부터 다시 반복
            });
        }
        public void Come()
        {
            MemberType target = coreRef.MoonData.Members[coreRef.NowTurn];
            bool gameEnd = false;
            if(target == MemberType.Dead && !coreRef.PlayerData.DownHeartCount()) gameEnd = true;
            coreRef.EnemyData.EditExpectList(coreRef.NowTurn, target);

            selectionItemUIRef.gameObject.SetActive(false);
            moonAnimRef.Play(AnimationType.SendToU, PlayerType.Player, target, () =>
            {
                if(target == MemberType.Dead)
                {
                    playerHeartUIRef.DownOneHeart();
                    if(gameEnd) 
                    {
                        GameOverUIRef.SetActive(true); 
                        return; 
                    }
                }

                if(coreRef.CheckTurnAndAddCount()) 
                {
                    if(target == MemberType.Dead) StartCoroutine(SetTurnUI(PlayerType.Enemy));
                    else {turnUIRef.SetNowTurn(coreRef.NowTurn+1); StartPlayerTurn();}
                }
                else Loop(); //턴 종료되면 턴 생성부터 다시 반복
            });
        }
        #endregion
    
        #region ### 적 액션 ###
        void UseBoxBA()
        {
            coreRef.EnemyData.UpHeartCount();
            coreRef.EnemyData.UseItem(Item.Box);

            moonAnimRef.Play(AnimationType.Box, PlayerType.Enemy, () =>
            {
                enemyItemUIRef.UseItem(Item.Box);
                enemyHeartUIRef.UpOneHeart();
                StartEnemyTurn();
            });
        }
        void UseMailBA()
        {
            //예상 리스트에 반영
            int targetIndex = coreRef.MoonData.GetFirstAliveOne() + coreRef.NowTurn;
            coreRef.EnemyData.EditExpectList(targetIndex, MemberType.Alive);
            coreRef.EnemyData.UseItem(Item.Mail);

            moonAnimRef.Play(AnimationType.Mail, PlayerType.Enemy, () =>
            {
                enemyItemUIRef.UseItem(Item.Mail);
                StartEnemyTurn();
            }); 
        }
        void UseBlackholeBA()
        {
            MemberType target = coreRef.MoonData.Members[coreRef.NowTurn];
            coreRef.EnemyData.EditExpectList(coreRef.NowTurn, target);
            coreRef.EnemyData.UseItem(Item.BlackHole);

            moonAnimRef.Play(AnimationType.BlackHole, PlayerType.Enemy, target, () =>
            {
                enemyItemUIRef.UseItem(Item.BlackHole);
                if(coreRef.CheckTurnAndAddCount()) { turnUIRef.SetNowTurn(coreRef.NowTurn+1); StartEnemyTurn(); }
                else Loop(); //턴 종료되면 턴 생성부터 다시 반복
            });
        }
        void UsePoisonBA()
        {
            MemberType target = coreRef.MoonData.Members[coreRef.NowTurn];
            if(target == MemberType.Alive) target = MemberType.Dead;
            else target = MemberType.Alive;
            coreRef.MoonData.Members[coreRef.NowTurn] = target;

            MemberType eTarget = coreRef.EnemyData.ExpectList[coreRef.NowTurn];
            if(eTarget != MemberType.Unknown)
            {
                if(eTarget == MemberType.Alive) eTarget = MemberType.Dead;
                else eTarget = MemberType.Alive;
                coreRef.EnemyData.EditExpectList(coreRef.NowTurn, eTarget);
            }
            coreRef.EnemyData.UseItem(Item.Poison);

            moonAnimRef.Play(AnimationType.Poison, PlayerType.Enemy, () =>
            {
                enemyItemUIRef.UseItem(Item.Poison);
                StartEnemyTurn();
            });
        }
        void SendBA()
        {
            MemberType target = coreRef.MoonData.Members[coreRef.NowTurn];
            coreRef.EnemyData.EditExpectList(coreRef.NowTurn, target);
            bool gameEnd = false;
            if(target == MemberType.Dead && !coreRef.PlayerData.DownHeartCount()) gameEnd = true;
            
            moonAnimRef.Play(AnimationType.SendToU, PlayerType.Enemy, target, () =>
            {
                if(target == MemberType.Dead)
                {
                    playerHeartUIRef.DownOneHeart();
                    if(gameEnd) 
                    {
                        GameOverUIRef.SetActive(true); //플레이어 기준
                        return; 
                    }
                }

                if(coreRef.CheckTurnAndAddCount()) StartCoroutine(SetTurnUI(PlayerType.Player));
                else Loop(); //턴 종료되면 턴 생성부터 다시 반복
            });
        }
        void ComeBA()
        {
            MemberType target = coreRef.MoonData.Members[coreRef.NowTurn];
            coreRef.EnemyData.EditExpectList(coreRef.NowTurn, target);
            bool gameEnd = false;
            if(target == MemberType.Dead && !coreRef.EnemyData.DownHeartCount()) gameEnd = true;

            moonAnimRef.Play(AnimationType.SendToE, PlayerType.Enemy, target, () =>
            {
                if(target == MemberType.Dead)
                {
                    enemyHeartUIRef.DownOneHeart();
                    if(gameEnd)
                    {
                        GameWinUIRef.SetActive(true); //플레이어 기준
                        return; 
                    }
                }

                if(coreRef.CheckTurnAndAddCount()) 
                {
                    if(target == MemberType.Dead) StartCoroutine(SetTurnUI(PlayerType.Player));
                    else { turnUIRef.SetNowTurn(coreRef.NowTurn+1); StartEnemyTurn(); }
                }
                else Loop(); //턴 종료되면 턴 생성부터 다시 반복
            });
        }
        #endregion
    }
}

