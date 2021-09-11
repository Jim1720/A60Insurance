using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using A60Insurance.Controllers;

namespace A60Insurance.StyleFeature
{

    public interface IScreenStyleManager
    { 
        public bool AreStylesActive(String screen);

        public void InsertStyleActiveSetting(bool active);
    }

    public  class ScreenStyleManager : IScreenStyleManager
    {

        List<String> activeScreenStyleList;

        bool _useStyles = false;

        public ScreenStyleManager()
        {
            activeScreenStyleList = new List<String>
            {
               "claim", "update"
            };
        }

        public void InsertStyleActiveSetting(bool active)
        {
            // controller calls this to reflect environment variable setting.
            _useStyles = active;
        }

        public  bool AreStylesActive(String screen)
        {


            return activeScreenStyleList.Contains(screen) && _useStyles;

        }
    }
}
