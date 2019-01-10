using System;
using Microsoft.SPOT;
using System.Collections;
using imBMW.Features.Menu.Screens;
using imBMW.Tools;
using imBMW.iBus.Devices.Emulators;
using imBMW.Multimedia;
using imBMW.Features.Localizations;
using System.Threading;
using imBMW.iBus;

namespace imBMW.Features.Menu
{
    public abstract class MenuBase
    {
        bool isEnabled;
        MenuScreen homeScreen;
        MenuScreen currentScreen;
        Stack navigationStack = new Stack();

        protected MediaEmulator mediaEmulator;

        public MenuBase(MediaEmulator mediaEmulator)
        {
            homeScreen = HomeScreen.Instance;
            CurrentScreen = homeScreen;

            this.mediaEmulator = mediaEmulator;
            mediaEmulator.IsEnabledChanged += mediaEmulator_IsEnabledChanged;
            mediaEmulator.PlayerIsPlayingChanged += ShowPlayerStatus;
            mediaEmulator.PlayerStatusChanged += ShowPlayerStatus;
            mediaEmulator.PlayerChanged += mediaEmulator_PlayerChanged;
            mediaEmulator_PlayerChanged(mediaEmulator.Player);

            Manager.AddMessageReceiverForSourceDevice(DeviceAddress.Radio, ProcessRadioMessage);
        }

        #region Radio members

        protected virtual void ProcessRadioMessage(Message m)
        {
            if (!IsEnabled)
            {
                return;
            }

            if (m.Data.Length == 3 && m.Data[0] == 0x38 && m.Data[1] == 0x0A)
            {
                switch (m.Data[2])
                {
                    case 0x00:
                        mediaEmulator.Player.Next();
                        m.ReceiverDescription = "Next track";
                        break;
                    case 0x01:
                        mediaEmulator.Player.Prev();
                        m.ReceiverDescription = "Prev track";
                        break;
                }
            }
        }

        #endregion

        #region MediaEmulator members

        Timer displayStatusDelayTimer;
        protected const int displayStatusDelay = 900; // TODO make abstract

        protected abstract int StatusTextMaxlen { get; }

        protected abstract void ShowPlayerStatus(IAudioPlayer player, string status, PlayerEvent playerEvent);

        protected abstract void ShowPlayerStatus(IAudioPlayer player, bool isPlaying);

        protected void ShowPlayerStatus(IAudioPlayer player)
        {
            // TODO move to player interface
            #if !MF_FRAMEWORK_VERSION_V4_1
            if (player is BluetoothWT32 && !((BluetoothWT32)player).IsConnected)
            {
                ShowPlayerStatus(player, Localization.Current.Disconnected, PlayerEvent.Wireless);
            }
            else
            #endif
            {
                ShowPlayerStatus(player, player.IsPlaying);
            }
        }

        protected void ShowPlayerStatus(IAudioPlayer player, string status)
        {
            if (!IsEnabled)
            {
                //return;
                Logger.Warning("Why shouldn't I set player status when menu is disabled?!");
            }
            if (displayStatusDelayTimer != null)
            {
                displayStatusDelayTimer.Dispose();
                displayStatusDelayTimer = null;
            }

            player.Menu.Status = status;
        }

        protected void ShowPlayerStatusWithDelay(IAudioPlayer player)
        {
            if (displayStatusDelayTimer != null)
            {
                displayStatusDelayTimer.Dispose();
                displayStatusDelayTimer = null;
            }

            displayStatusDelayTimer = new Timer(delegate
            {
                ShowPlayerStatus(player);
            }, null, displayStatusDelay, 0);
        }

        protected string TextWithIcon(string icon, string text = null)
        {
            if (StringHelpers.IsNullOrEmpty(text))
            {
                return icon;
            }
            if (icon.Length + text.Length < StatusTextMaxlen)
            {
                return icon + " " + text;
            }
            return icon + text;
        }

        protected string TextWithIcon(char icon, string text = null)
        {
            if (StringHelpers.IsNullOrEmpty(text))
            {
                return icon + "";
            }
            if (text.Length + 1 < StatusTextMaxlen)
            {
                return icon + " " + text;
            }
            return icon + text;
        }

        void mediaEmulator_PlayerChanged(IAudioPlayer player)
        {
            HomeScreen.Instance.PlayerScreen = player.Menu;
        }

        void mediaEmulator_IsEnabledChanged(MediaEmulator emulator, bool isEnabled)
        {
            IsEnabled = isEnabled;
        }

        #endregion

        #region Drawing members

        protected virtual void DrawScreen(MenuScreenUpdateEventArgs args) { }

        protected virtual void ScreenSuspend()
        {
            ScreenNavigatedFrom(CurrentScreen);
        }

        protected virtual void ScreenWakeup()
        {
            ScreenNavigatedTo(CurrentScreen, false);
        }

        public virtual void UpdateScreen(MenuScreenUpdateReason reason, object item = null)
        {
            UpdateScreen(new MenuScreenUpdateEventArgs(reason, item));
        }

        public virtual void UpdateScreen(MenuScreenUpdateEventArgs args)
        {
            if (!IsEnabled)
            {
                return;
            }
            DrawScreen(args);
        }

        void currentScreen_Updated(MenuScreen screen, MenuScreenUpdateEventArgs args)
        {
            UpdateScreen(args);
        }

        #endregion

        #region Navigation members

        public bool IsEnabled
        {
            get { return isEnabled; }
            set
            {
                if (isEnabled == value)
                {
                    return;
                }
                isEnabled = value;
                if (value)
                {
                    ScreenWakeup();
                    UpdateScreen(MenuScreenUpdateReason.Navigation);
                }
                else
                {
                    ScreenSuspend();
                }
            }
        }

        public void Navigate(MenuScreen screen)
        {
            if (screen == null)
            {
                Logger.Error("Navigation to null screen");
                return;
            }
            if (CurrentScreen == screen)
            {
                return;
            }
            navigationStack.Push(CurrentScreen);
            CurrentScreen = screen;
        }

        public void NavigateBack()
        {
            if (navigationStack.Count > 0)
            {
                CurrentScreen = (MenuScreen)navigationStack.Pop();
            }
            else
            {
                NavigateHome();
            }
        }

        public void NavigateHome()
        {
            CurrentScreen = homeScreen;
            navigationStack.Clear();
        }

        public void NavigateAfterHome(MenuScreen screen)
        {
            navigationStack.Clear();
            navigationStack.Push(homeScreen);
            CurrentScreen = screen;
        }

        public MenuScreen CurrentScreen
        {
            get
            {
                return currentScreen;
            }
            set
            {
                if (currentScreen == value || value == null)
                {
                    return;
                }
                ScreenNavigatedFrom(currentScreen);
                currentScreen = value;
                ScreenNavigatedTo(currentScreen, true);
                UpdateScreen(MenuScreenUpdateReason.Navigation);
            }
        }

        protected virtual void ScreenNavigatedTo(MenuScreen screen, bool fromAnotherScreen)
        {
            if (screen == null || !screen.OnNavigatedTo(this))
            {
                return;
            }

            screen.ItemClicked += currentScreen_ItemClicked;
            screen.Updated += currentScreen_Updated;
        }

        protected virtual void ScreenNavigatedFrom(MenuScreen screen)
        {
            if (screen == null)
            {
                return;
            }

            screen.OnNavigatedFrom(this);

            screen.ItemClicked -= currentScreen_ItemClicked;
            screen.Updated -= currentScreen_Updated;
        }

        void currentScreen_ItemClicked(MenuScreen screen, MenuItem item)
        {
            switch (item.Action)
            {
                case MenuItemAction.GoToScreen:
                    Navigate(item.GoToScreen);
                    break;
                case MenuItemAction.GoBackScreen:
                    NavigateBack();
                    break;
                case MenuItemAction.GoHomeScreen:
                    NavigateHome();
                    break;
            }
        }

        #endregion
    }
}
