using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Prism.ViewModel;
using System.Collections.ObjectModel;

namespace NavigateLaser.Models
{
    class TreeRootM : NotificationObject
    {
        static readonly TreeRootM DummyChild = new TreeRootM();

        private bool _isExpanded;
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (value != _isExpanded)
                {
                    _isExpanded = value;
                    this.RaisePropertyChanged("IsExpanded");
                }

                if (_isExpanded && _parent != null)
                    _parent.IsExpanded = true;

                if (this.HasDummyChild)
                {
                    this.Children.Remove(DummyChild);
                    this.LoadChildren();
                }
            }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (value != _isSelected)
                {
                    _isSelected = value;
                    this.RaisePropertyChanged("IsSelected");
                }
            }
        }

        private TreeRootM _parent;
        public TreeRootM Parent
        {
            get { return _parent; }
            set
            {
                _parent = value;
                this.RaisePropertyChanged("Parent");
            }
        }

        private ObservableCollection<TreeRootM> _children;
        public ObservableCollection<TreeRootM> Children
        {
            get { return _children; }
            set
            {
                _children = value;
                this.RaisePropertyChanged("Children");
            }
        }

        public TreeRootM()
        {

        }

        public TreeRootM(TreeRootM parent, bool lazyLoadChildren)
        {
            _parent = parent;

            _children = new ObservableCollection<TreeRootM>();

            if (lazyLoadChildren)
                _children.Add(DummyChild);
        }

        public bool HasDummyChild
        {
            get { return this.Children.Count == 1 && this.Children[0] == DummyChild; }
        }

        protected virtual void LoadChildren()
        {
        }
    }
}
