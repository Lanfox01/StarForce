//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework.ObjectPool;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace StarForce
{
    /// <summary>
    /// HPBarComponent类通过对象池技术高效地管理游戏中生命值条的显示和隐藏，减少了内存分配和回收的开销，提高了游戏的性能。
    /// </summary>
    public class HPBarComponent : GameFrameworkComponent
    {
        [SerializeField] private HPBarItem m_HPBarItemTemplate = null; //生命值条模板的引用，用于创建新的生命值条实例

        [SerializeField] private Transform m_HPBarInstanceRoot = null; // 生命值条实例的根节点，所有动态创建的生命值条都会挂载到这个节点下

        [SerializeField] private int m_InstancePoolCapacity = 16; //对象池中生命值条对象的最大容量。

        private IObjectPool<HPBarItemObject> m_HPBarItemObjectPool = null; //生命值条对象的对象池实例，用于管理生命值条对象的创建、回收和复用
        private List<HPBarItem> m_ActiveHPBarItems = null; //当前活跃的生命值条列表，存储了所有正在显示的生命值条。
        private Canvas m_CachedCanvas = null; // 缓存的Canvas组件，用于快速访问根节点下的Canvas，以便进行UI相关的操作。

        private void Start()
        {
            if (m_HPBarInstanceRoot == null)
            {
                Log.Error("You must set HP bar instance root first.");
                return;
            }

            m_CachedCanvas = m_HPBarInstanceRoot.GetComponent<Canvas>();
            m_HPBarItemObjectPool =
                GameEntry.ObjectPool.CreateSingleSpawnObjectPool<HPBarItemObject>("HPBarItem", m_InstancePoolCapacity);
            m_ActiveHPBarItems = new List<HPBarItem>();
        }

        private void OnDestroy()
        {
        }

        private void Update()
        {
            //调用每个生命值条的Refresh方法，如果返回true（表示生命值条仍然需要显示），则继续遍历；
            //如果返回false（表示生命值条不再需要显示），则调用HideHPBar方法隐藏该生命值条
            for (int i = m_ActiveHPBarItems.Count - 1; i >= 0; i--)
            {
                HPBarItem hpBarItem = m_ActiveHPBarItems[i];
                if (hpBarItem.Refresh())
                {
                    continue;
                }

                HideHPBar(hpBarItem);
            }
        }

        /// <summary>
        /// 此方法预期用于显示一个生命值条，接收一个Entity（实体）和两个浮点数（fromHPRatio和toHPRatio）作为参数，
        /// 分别表示生命值变化的起始比例和结束比例
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="fromHPRatio"></param>
        /// <param name="toHPRatio"></param>
        public void ShowHPBar(Entity entity, float fromHPRatio, float toHPRatio)
        {
            if (entity == null)
            {
                Log.Warning("Entity is invalid.");
                return;
            }

            HPBarItem hpBarItem = GetActiveHPBarItem(entity);
            if (hpBarItem == null)
            {
                hpBarItem = CreateHPBarItem(entity);
                m_ActiveHPBarItems.Add(hpBarItem);
            }

            hpBarItem.Init(entity, m_CachedCanvas, fromHPRatio, toHPRatio);
        }

        private void HideHPBar(HPBarItem hpBarItem)
        {
            hpBarItem.Reset();
            m_ActiveHPBarItems.Remove(hpBarItem);
            m_HPBarItemObjectPool.Unspawn(hpBarItem);
        }

        private HPBarItem GetActiveHPBarItem(Entity entity)
        {
            if (entity == null)
            {
                return null;
            }

            for (int i = 0; i < m_ActiveHPBarItems.Count; i++)
            {
                if (m_ActiveHPBarItems[i].Owner == entity)
                {
                    return m_ActiveHPBarItems[i];
                }
            }

            return null;
        }

        private HPBarItem CreateHPBarItem(Entity entity)
        {
            HPBarItem hpBarItem = null;
            HPBarItemObject hpBarItemObject = m_HPBarItemObjectPool.Spawn();
            if (hpBarItemObject != null)
            {
                hpBarItem = (HPBarItem) hpBarItemObject.Target;
            }
            else
            {
                hpBarItem = Instantiate(m_HPBarItemTemplate);
                Transform transform = hpBarItem.GetComponent<Transform>();
                transform.SetParent(m_HPBarInstanceRoot);
                transform.localScale = Vector3.one;
                m_HPBarItemObjectPool.Register(HPBarItemObject.Create(hpBarItem), true);
            }

            return hpBarItem;
        }
    }
}