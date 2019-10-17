using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Prism.ViewModel;

namespace NavigateLaser.Models
{
    class TreeSecondM : NotificationObject
    {
        public TreeSecondM(string SecondTreeName)
        {
            this.SecondTreeName = SecondTreeName;
        }

        public string SecondTreeName { get; private set; }

    }
}
