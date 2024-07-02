//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using GameFramework.DataTable;
using UnityEngine;

namespace StarForce
{
    public class SurvivalGame : GameBase
    {
        private float m_ElapseSeconds = 0f;

        public override GameMode GameMode
        {
            get
            {
                return GameMode.Survival;
            }
        }

        public override void Update(float elapseSeconds, float realElapseSeconds)
        {
            base.Update(elapseSeconds, realElapseSeconds);

            m_ElapseSeconds += elapseSeconds;
            if (m_ElapseSeconds >= 1f) // 检查m_ElapseSeconds是否达到或超过1秒  
            {
                m_ElapseSeconds = 0f;
                // 从游戏的数据表中获取小行星（DRAsteroid）的数据表  
                IDataTable<DRAsteroid> dtAsteroid = GameEntry.DataTable.GetDataTable<DRAsteroid>();
                float randomPositionX = SceneBackground.EnemySpawnBoundary.bounds.min.x + SceneBackground.EnemySpawnBoundary.bounds.size.x * (float)Utility.Random.GetRandomDouble();
                float randomPositionZ = SceneBackground.EnemySpawnBoundary.bounds.min.z + SceneBackground.EnemySpawnBoundary.bounds.size.z * (float)Utility.Random.GetRandomDouble();
                // 在游戏世界中显示一个小行星，使用新创建的AsteroidData对象作为参数  
                // AsteroidData的构造参数包括一个序列号（通过GameEntry.Entity.GenerateSerialId()生成）和一个小行星的ID（从dtAsteroid的计数中随机选择一个，并加上一个偏移量60000）  
                // AsteroidData还设置了小行星的位置为上面计算出的随机位置  
                GameEntry.Entity.ShowAsteroid(new AsteroidData(GameEntry.Entity.GenerateSerialId(), 60000 + Utility.Random.GetRandom(dtAsteroid.Count))
                {
                    Position = new Vector3(randomPositionX, 0f, randomPositionZ),
                });
            }
        }
    }
}
