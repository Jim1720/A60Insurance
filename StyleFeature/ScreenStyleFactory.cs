using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using A60Insurance.StyleFeature;
using A60Insurance.Controllers;

namespace A60Insurance.StyleFeature
{ 
    public interface IScreenStyleFactory
    {
         
         public String getStyleLinkValue(String screenName); 
         public String getColorLinkValue(String screenName);
         public void getNextStyle(String screenName);
         public void getNextColor(String screenName); 
         public ScreenStyleObject getCurrentStyleForScreen(String screenName); 
    }

    public class ScreenStyleFactory : IScreenStyleFactory
    {
        // Dependency Injection Chain - add the ScreenStyleMananager and the ScreenStyleList
         
        public readonly IScreenStyleList _screenStyleList;

        public ScreenStyleFactory(IScreenStyleList screenStyleList) 
        { 

            _screenStyleList = screenStyleList; 
        } 
         

        List<String> userColors = new List<String>
        {
            "white", "red" , "pink","blue","aqua","yellow","green","gold" 
        };

        List<String> labelColors = new List<String>
        {
             "black" , "white","red","white","blue","black","white","brown"
        };

        List<String> headerColors = new List<String>
        {
            "black" , "white", "red","white","blue","black","white","brown"
        };

        List<String> messageColors = new List<String>
        { 
            "black" , "white", "red","white","blue","black","white","brown"
        };

        List<String> internalClasses = new List<String>
        {
            "Style", "Picture", "Outline", "Solid"
        };

        List<String> externalClasses = new List<String>
        {
            "bg-style", "bg-image", "bg-outline", "bg-solid"
        };

        String defaultHeaderColor = "white";
        String defaultLabelColor = "dodgerblue";
        String defaultMessageColor = "gold";
        String defaultPictureLabelColor = "gold";
        String defaultPictureMessageColor = "gold";

        private void AddNewStyleObject(String currentScreen)
        {
            ScreenStyleObject screenStyleObject = new ScreenStyleObject()
            {

                // add cycle #2 = picture.

                screen = currentScreen,
                internalClass = "Picture",
                externalClass = "bg-image",
                userColor = "white", 
                labelColor = "white",
                headerColor = "white",
                messageColor = "white" 
            };

            _screenStyleList.add(screenStyleObject);

        }

        public String getStyleLinkValue(String screenName)
        {

            var screenStyleObject = _screenStyleList.Find(screenName);
            return screenStyleObject.externalClass; // will be blank if not found

        }

        public String getColorLinkValue(String screenName)
        {
            var screenStyleObject = _screenStyleList.Find(screenName);
            return screenStyleObject.userColor; // will be blank if not found
        }

        public void getNextStyle(String screenName)
        { 
            var screenStyleObject = _screenStyleList.Find(screenName);
            var situation = screenStyleObject == null ? "New" : "Existing";
            if(situation == "New")
            {
                AddNewStyleObject(screenName);
                return;
            }
            var max = this.internalClasses.Count;
            var currentInternalClass = screenStyleObject.internalClass;
            var match = false;
            for (var i = 0; !match && i < max; i++)
            {
                if (currentInternalClass == this.internalClasses[i])
                {
                    var endOfList = i == max - 1;
                    var next = endOfList ? 0 : i + 1;
                    screenStyleObject.internalClass = this.internalClasses[next];
                    screenStyleObject.externalClass = this.externalClasses[next]; 
                    // setup up colors take first in list
                    var first = 0;
                    if (screenStyleObject.internalClass == "Solid")  
                    {
                        screenStyleObject.userColor = userColors[first];
                        screenStyleObject.headerColor = headerColors[first];
                        screenStyleObject.labelColor = labelColors[first];
                        screenStyleObject.messageColor = messageColors[first];
                    }
                    else if(screenStyleObject.internalClass == "Picture")
                    {
                        screenStyleObject.headerColor = defaultHeaderColor;
                        screenStyleObject.labelColor = defaultPictureLabelColor;
                        screenStyleObject.messageColor = defaultPictureMessageColor;
                    }
                    else
                    {
                        // outline and Style have same values for black background.
                        screenStyleObject.headerColor = defaultHeaderColor;
                        screenStyleObject.labelColor = defaultLabelColor;
                        screenStyleObject.messageColor = defaultMessageColor;
                    }
                    _screenStyleList.replace(screenStyleObject);
                    match = true;
                }
            }
        }

        public void getNextColor(String screenName)
        {

            var screenStyleObject = _screenStyleList.Find(screenName);
            var max = userColors.Count;
            var match = false;
            for(var i = 0; match == false && i < max; i++)
            {

               if(screenStyleObject.userColor == userColors[i])
               {

                    match = true;
                    var endOfList = i == max - 1;
                    var next = endOfList ? 0 : i + 1;
                    screenStyleObject.userColor = userColors[next];
                    if(screenStyleObject.internalClass == "Solid")
                    {
                        screenStyleObject.headerColor = headerColors[next];
                        screenStyleObject.labelColor = labelColors[next];
                        screenStyleObject.messageColor = messageColors[next];
                    }
                    else if (screenStyleObject.internalClass == "Picture")
                    {
                        screenStyleObject.headerColor = defaultHeaderColor;
                        screenStyleObject.labelColor = defaultPictureLabelColor;
                        screenStyleObject.messageColor = defaultPictureMessageColor;
                    }
                    else
                    {
                        screenStyleObject.headerColor = defaultHeaderColor;
                        screenStyleObject.labelColor = defaultLabelColor;
                        screenStyleObject.messageColor = defaultMessageColor;
                    }
                    
               } 
            }
            _screenStyleList.replace(screenStyleObject);
        }

        public ScreenStyleObject getCurrentStyleForScreen(String screenName)
        {
             
            var screenStyleObject = _screenStyleList.Find(screenName);
            return screenStyleObject;


        }
    }
}
