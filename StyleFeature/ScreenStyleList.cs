using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using A60Insurance.StyleFeature;

namespace A60Insurance.StyleFeature
{

    public interface IScreenStyleList
    {

        public void add(ScreenStyleObject screenStyleObject);
        public void replace(ScreenStyleObject screenStyleObject); 
        public ScreenStyleObject Find(String screenName);
        public List<ScreenStyleObject> getScreenStyleList();
        public void setScreenStyleList(List<ScreenStyleObject> list);

        public Boolean isEmpty();

    }
    public  class ScreenStyleList : IScreenStyleList
    {

        private  List<ScreenStyleObject> screenStyleList;

       
        public ScreenStyleList()
        {

            screenStyleList = new List<ScreenStyleObject>();

        } 
        public  void add(ScreenStyleObject screenStyleObject)
        {
            screenStyleList.Add(screenStyleObject);
        }
        public  void replace(ScreenStyleObject screenStyleObject)
        {
    
            var index = screenStyleList.FindIndex(s => s.screen == screenStyleObject.screen);
            screenStyleList.RemoveAt(index);
            screenStyleList.Insert(index, screenStyleObject); 

        }

        public  ScreenStyleObject Find(String screenName)
        {
            return screenStyleList.Find(s => s.screen == screenName);
        }

        /* persistent operations - main controller responsible */

        public List<ScreenStyleObject> getScreenStyleList()
        {
            return screenStyleList;
        }

        public void setScreenStyleList(List<ScreenStyleObject> list)
        {
            this.screenStyleList = list;
        }

        public bool isEmpty()
        {
            return this.screenStyleList.Count == 0;
        }

     
    }
}
