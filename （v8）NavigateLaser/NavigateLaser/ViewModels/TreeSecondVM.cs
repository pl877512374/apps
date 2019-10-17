using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NavigateLaser.Models;

namespace NavigateLaser.ViewModels
{
    class TreeSecondVM : TreeRootM
    {
        private TreeSecondM _SecondTree;

        public TreeSecondVM(TreeSecondM SecondTree, TreeFirstVM parentState)
            : base(parentState, false)
        {
            _SecondTree = SecondTree;
        }

        public string SecondTreeName
        {
            get { return _SecondTree.SecondTreeName; }
        }
    }
}
