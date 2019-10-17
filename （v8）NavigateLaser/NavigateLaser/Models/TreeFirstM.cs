using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using Microsoft.Practices.Prism.ViewModel;

namespace NavigateLaser.Models
{
    class TreeFirstM : NotificationObject
    {
        public TreeFirstM(string FirstTreeName)
        {
            this.FirstTreeName = FirstTreeName;
        }

        public string FirstTreeName { get; private set; }

        private ObservableCollection<TreeSecondM> _SecondTrees = new ObservableCollection<TreeSecondM>();
        public ObservableCollection<TreeSecondM> SecondTrees
        {
            get { return _SecondTrees; }
        }
    }
}
