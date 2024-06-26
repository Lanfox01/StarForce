﻿//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using UnityEngine;

namespace StarForce
{
    /// <summary>
    /// 游戏入口。还是框架入口？
    /// </summary>
    public partial class GameEntry : MonoBehaviour
    {
        private void Start()
        {
            InitBuiltinComponents();  //初始化框架自带的基础组件；
            InitCustomComponents();   // 初始化 自己的组件
        }
    }
}
