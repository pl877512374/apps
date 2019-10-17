using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NavigateLaser.Models;
using NavigateLaser.DataAccess;

namespace NavigateLaser.ViewModels
{
    class TreeFirstVM : TreeRootM
    {
        TreeData m_TreeData = new TreeData();
        private TreeFirstM _FirstTree;
        private int _SecondTreeType;

        public TreeFirstVM(TreeFirstM FirstTree, int SecondTreeType)
            : base(null, true)
        {
            _FirstTree = FirstTree;
            _SecondTreeType = SecondTreeType;
        }

        public string FirstTreeName
        {
            get { return _FirstTree.FirstTreeName; }
        }

        protected override void LoadChildren()
        {
            if (_SecondTreeType == 1)
            {
                foreach (TreeSecondM SecondTree in m_TreeData.GetSecondTrees(_FirstTree))
                    base.Children.Add(new TreeSecondVM(SecondTree, this));
            }
            else
            {
                foreach (TreeSecondM SecondTree in m_TreeData.GetSecondTreesNotLogin(_FirstTree))
                    base.Children.Add(new TreeSecondVM(SecondTree, this));
            }
            
        }
    }
}
