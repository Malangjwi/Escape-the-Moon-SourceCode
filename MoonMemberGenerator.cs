
using System.Collections.Generic;
using AttributesBox;
using UnityEngine;

namespace SpaceToyProject.Core
{
    public enum MemberType
    {
        Alive,
        Dead,
        Unknown
    }
    [System.Serializable]
    public class MoonData
    {
        [ReadOnly][SerializeField] int selectedMember;
        [ReadOnly][SerializeField] List<MemberType> members;
        public List<MemberType> Members => members;
        public MemberType GetNowRealMember() { return members[CoreManager.Instance.NowTurn]; }
        public int SelectedMember => selectedMember;
        public int SelectedDeadMember //poison 아이템 사용 관련 문제 수정 
        {
            get
            {
                int counter = 0;
                foreach (MemberType member in members) { counter += member == MemberType.Dead ? 1 : 0; }
                return counter;
            }
        }
        public int SelectedAliveMember 
        {
            get
            {
                int counter = 0;
                foreach (MemberType member in members) { counter += member == MemberType.Alive ? 1 : 0; }
                return counter;
            }
        }
        public MoonData(List<MemberType> members, int selectedMember)
        {
            this.members = members;
            this.selectedMember = selectedMember;
        }

        //액션용
        /// <summary>
        /// 지금 턴에서 가장 앞순위에 있는 생존자의 인덱스를 반환합니다
        /// </summary>
        /// <returns>반환되는 수가 음수이면 남은 생존자는 없습니다</returns>
        public int GetFirstAliveOne()
        {
            int nowTurn = CoreManager.Instance.NowTurn;
            for(int i = nowTurn; i < selectedMember; i++)
            {
                if(members[i] == MemberType.Alive) return i - nowTurn;
            }
            return -1;
        }
    }

    public class MoonMemberGenerator : MonoBehaviour
    {
        [Header("세팅 값")]
        [HorizontalLine]
        [SerializeField] int baseCount = 5;
        [SerializeField] int randomRate = 1;
        [SerializeField][Range(0f, 1f)] float minDeadRate = 0.2f;
        [SerializeField][Range(0f, 1f)] float maxDeadRate = 0.7f;

  
        [Header("결과")]
        [HorizontalLine]
        [ReadOnly][SerializeField] int selectedCount;
        [ReadOnly][SerializeField][Range(0f, 1f)] float selectedDeadRate;
        [ReadOnly][SerializeField] int deadCount;
        [HorizontalLine]
        [ReadOnly][SerializeField] List<MemberType> members;
        
        /// <summary>
        /// 맴버 수를 선정합니다
        /// </summary>
        [ContextMenu("Debug(Select)")]
        void SelectMember()
        {
            selectedCount = Random.Range(baseCount - randomRate, baseCount + randomRate + 1);
            selectedDeadRate = Random.Range(minDeadRate, maxDeadRate);
            deadCount = Mathf.RoundToInt(selectedCount * selectedDeadRate);
        }
        /// <summary>
        /// 선정한 수로 맴버를 생성하고 섞습니다
        /// </summary>
        [ContextMenu("Debug(Shake)")]
        void ShakeMemeber()
        {
            int tempDeadCount = deadCount;
            List<MemberType> tempMember = new List<MemberType>(selectedCount);

            for(int i = 0; i < selectedCount; i++)
            {
                if(tempDeadCount > 0)
                {
                    tempMember.Add(MemberType.Dead);
                    --tempDeadCount;
                }
                else tempMember.Add(MemberType.Alive);
            }

            for(int i = 0; i < selectedCount; i++)
            {
                int rm = Random.Range(0, selectedCount);
                //Debug.Log($"rm {rm}: {tempMember[rm]}, i {i}: {tempMember[i]}");
                MemberType tempM = tempMember[rm];
                tempMember[rm] = tempMember[i];
                tempMember[i] = tempM;
            }
            members = tempMember;
        }

        #region  ### 외부에서 호출 ###
        /// <summary>
        /// 외부에서 요청
        /// </summary>
        /// <returns></returns>
        public MoonData GetMoonMembers()
        {
            SelectMember();
            ShakeMemeber();
            return new MoonData(members, selectedCount);
        }
        #endregion
    }
}

