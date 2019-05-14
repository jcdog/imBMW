using System;
using Microsoft.SPOT;

namespace imBMW.Features.Menu
{
    public enum MenuItemAction
    {
        None,
        GoToScreen,
        GoBackScreen,
        GoHomeScreen,
        Refresh,
        PassiveCheckbox
    }

    public enum MenuItemType
    {
        Text,
        Button,
        Checkbox
    }

    public delegate string GetTextHandler(MenuItem item);

    public delegate void MenuItemEventHandler(MenuItem item);

    public class MenuItem
    {
        private string text;
        private bool isChecked;

        private GetTextHandler getTextCallback;

        public MenuItem(string text, MenuItemType type = MenuItemType.Text, MenuItemAction action = MenuItemAction.None)
        {
            Text = text;
            Type = type;
            Action = action;
        }

        public MenuItem(string text, MenuItemEventHandler callback, MenuItemType type = MenuItemType.Button, MenuItemAction action = MenuItemAction.None)
            : this(text, type, action)
        {
            Clicked += callback;
        }

        public MenuItem(GetTextHandler getTextCallback, MenuItemType type = MenuItemType.Text, MenuItemAction action = MenuItemAction.None)
            : this(string.Empty, type, action)
        {
            this.getTextCallback = getTextCallback;
        }

        public MenuItem(GetTextHandler getTextCallback, MenuItemEventHandler callback, MenuItemType type = MenuItemType.Button, MenuItemAction action = MenuItemAction.None)
            : this(getTextCallback, type, action)
        {
            Clicked += callback;
        }

        public string RadioAbbreviation { get; set; }

        public string Text
        {
            get
            {
                if (getTextCallback != null)
                {
                    Text = getTextCallback(this);
                }
                return text;
            }
            set
            {
                if (text == value)
                {
                    return;
                }
                text = value;
                Refresh();
            }
        }

        public bool IsChecked
        {
            get { return isChecked; }
            set
            {
                if (isChecked == value)
                {
                    return;
                }
                isChecked = value;
                OnCheckedChanged();
            }
        }

        public MenuItemAction Action { get; private set; }

        public MenuItemType Type { get; private set; }

        public MenuScreen GoToScreen { get; set; }

        public void Refresh()
        {
            OnChanged();
        }

        public void Click()
        {
            OnClicked();
        }

        public event MenuItemEventHandler Changed;

        public event MenuItemEventHandler IsCheckedChanged;

        public event MenuItemEventHandler Clicked;

        protected void OnChanged()
        {
            var e = Changed;
            if (e != null)
            {
                e(this);
            }
        }

        protected void OnCheckedChanged()
        {
            var e = IsCheckedChanged;
            if (e != null)
            {
                e(this);
            }

            OnChanged();
        }

        protected void OnClicked()
        {
            switch (Type)
            {
                case MenuItemType.Checkbox:
                    if (Action != MenuItemAction.PassiveCheckbox)
                    {
                        IsChecked = !IsChecked;
                    }
                    break;
            }

            var e = Clicked;
            if (e != null)
            {
                e(this);
            }

            switch (Action)
            {
                case MenuItemAction.Refresh:
                    Refresh();
                    break;
            }
        }

        public override string ToString()
        {
            return Text;
        }
    }
}
