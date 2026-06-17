using UnityEngine;
using AttributesBox;
using System.Collections.Generic;

namespace SpaceToyProject.Core
{
    public enum Item { BlackHole, Poison, Mail, Box } //수정 시 ItemListGenerator.itemSortCount도 수정
    [System.Serializable]
    public struct ItemRate
    {
        public Item type;
        [Range(0f, 1f)] public float dropRate;
    }
    public class ItemListGenerator : MonoBehaviour
    {
        const int itemSortCount = 4;

        [Header("세팅 값")][HorizontalLine]
        [SerializeField] int minItemCount;
        [SerializeField] int maxItemCount;
        [HorizontalLine]
        [SerializeField] List<ItemRate> itemsRate;

        [Header("디버깅 결과")][HorizontalLine]
        [ReadOnly][SerializeField] List<Item> itemList;

        [ContextMenu("Debug(GenerateDebug)")]
        void GenerateDebug()
        {
            itemList = GenerateList();
        }
        List<Item> GenerateList()
        {
            //*추후 맴버 수에 따라 아이템 배분 갯수도 조정
            int selectedCount = Random.Range(minItemCount, maxItemCount+1);
            List<Item> tempitemList = new List<Item>(selectedCount);
            float itemsRateBase = 0;
            for(int i = 0; i < itemsRate.Count; i++)
            {
                itemsRateBase += itemsRate[i].dropRate;
            }
            for(int i = 0; i < selectedCount; i++)
            {
                float rate = Random.Range(0, itemsRateBase);
                float tempRate = 0;
                for(int j = 0; j < itemsRate.Count; j++)
                {
                    tempRate += itemsRate[j].dropRate;
                    //Debug.Log(j + ", " + tempRate + ", " + rate);
                    if (tempRate > rate)
                    {   
                        tempitemList.Add(itemsRate[j].type); 
                        break;
                    }
                }
            }
            return tempitemList;
        }

        #region ### 외부에서 호출 ###
        public List<Item> GetItemList()
        {
            return GenerateList();
        }
        #endregion
    }
}