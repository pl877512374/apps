using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NavigateLaser.Models;

namespace NavigateLaser.DataAccess
{
    class TreeData
    {
        public TreeFirstM[] GetFirstTrees()
        {
            return new TreeFirstM[]
            {
                new TreeFirstM("设备参数"),
                new TreeFirstM("扫描数据"),
                new TreeFirstM("靶标数据")
            };
        }

        public TreeSecondM[] GetSecondTrees(TreeFirstM FirstTree)
        {
            switch (FirstTree.FirstTreeName)
            {
                case "设备参数":
                    return new TreeSecondM[]
                    {
                        new TreeSecondM("基本参数"),
                        new TreeSecondM("出产参数"),
                        new TreeSecondM("功能参数")
                    };

                case "扫描数据":
                    return new TreeSecondM[]
                    {
                        new TreeSecondM("3D显示"),
                        new TreeSecondM("扫描信息")
                    };

                case "靶标数据":
                    return new TreeSecondM[]
                    {
                        new TreeSecondM("靶标设置")
                    };
            }

            return null;
        }

        public TreeFirstM[] GetFirstTreesNotLogin()
        {
            return new TreeFirstM[]
            {
                new TreeFirstM("设备参数")
            };
        }

        public TreeSecondM[] GetSecondTreesNotLogin(TreeFirstM FirstTree)
        {
            switch (FirstTree.FirstTreeName)
            {
                case "设备参数":
                    return new TreeSecondM[]
                    {
                        new TreeSecondM("基本参数"),
                        new TreeSecondM("出产参数"),
                    };
            }
            return null;
        }
    }
}
