using System;
using System.Collections.Generic;
using UnityEngine;
using YARG.Data;
using YARG.PlayMode;

namespace YARG.Input
{
    public class KeysInputStrategy : InputStrategy
    {
        public const string GREEN = "green";
        public const string RED = "red";
        public const string YELLOW = "yellow";
        public const string BLUE = "blue";
        public const string ORANGE = "orange";

        public const string WHAMMY = "whammy";

        public const string UP = "up";
        public const string DOWN = "down";

        public const string STAR_POWER = "star_power";
        public const string TILT = "tilt";
        public const string PAUSE = "pause";

        private List<NoteInfo> botChart;

        public delegate void FretChangeAction(bool pressed, int fret);

        public event FretChangeAction FretChangeEvent;

        public event Action StrumEvent;

        public delegate void WhammyChangeAction(float delta);

        public event WhammyChangeAction WhammyEvent;

        public KeysInputStrategy()
        {
            InputMappings = new()
            {
                {
                    GREEN, new(BindingType.BUTTON, "Green", GREEN)
                },
                {
                    RED, new(BindingType.BUTTON, "Red", RED)
                },
                {
                    YELLOW, new(BindingType.BUTTON, "Yellow", YELLOW)
                },
                {
                    BLUE, new(BindingType.BUTTON, "Blue", BLUE)
                },
                {
                    ORANGE, new(BindingType.BUTTON, "Orange", ORANGE)
                },
                {
                    WHAMMY, new(BindingType.AXIS, "Whammy", WHAMMY)
                },
                {
                    UP, new(BindingType.BUTTON, "Up", UP)
                },
                {
                    DOWN, new(BindingType.BUTTON, "Down", DOWN)
                },
                {
                    STAR_POWER, new(BindingType.BUTTON, "Star Power", STAR_POWER)
                },
                {
                    TILT, new(BindingType.BUTTON, "Tilt", TILT)
                }, // tilt is a button as PS2 guitars don't have a tilt axis
                {
                    PAUSE, new(BindingType.BUTTON, "Pause", PAUSE)
                },
            };
        }

        public override string GetIconName()
        {
            return "keys";
        }

        public override void InitializeBotMode(object rawChart)
        {
            botChart = (List<NoteInfo>)rawChart;
        }

        protected override void UpdatePlayerMode()
        {
            void HandleFret(string mapping, int index)
            {
                if (WasMappingPressed(mapping))
                {
                    FretChangeEvent?.Invoke(true, index);
                    StrumEvent?.Invoke();
                }
                else if (WasMappingReleased(mapping))
                {
                    FretChangeEvent?.Invoke(false, index);
                }
            }

            // Deal with fret inputs

            HandleFret(GREEN, 0);
            HandleFret(RED, 1);
            HandleFret(YELLOW, 2);
            HandleFret(BLUE, 3);
            HandleFret(ORANGE, 4);

            // Whammy!
            float currentWhammy = GetMappingValue(WHAMMY);
            float deltaWhammy = currentWhammy - GetPreviousMappingValue(WHAMMY);
            if (!Mathf.Approximately(deltaWhammy, 0f))
            {
                WhammyEvent?.Invoke(deltaWhammy);
            }

            // Starpower

            if (WasMappingPressed(STAR_POWER) || WasMappingPressed(TILT))
            {
                // checking for tilt
                CallStarpowerEvent();
            }
        }

        protected override void UpdateBotMode()
        {
            if (botChart == null)
            {
                return;
            }

            float songTime = Play.Instance.SongTime;

            bool resetForChord = false;
            while (botChart.Count > BotChartIndex && botChart[BotChartIndex].time <= songTime)
            {
                // Release old frets
                if (!resetForChord)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        FretChangeEvent?.Invoke(false, i);
                    }

                    resetForChord = true;
                }

                var noteInfo = botChart[BotChartIndex];
                BotChartIndex++;

                // Skip fret press if open note
                if (noteInfo.fret != 5)
                {
                    FretChangeEvent?.Invoke(true, noteInfo.fret);
                }

                // Strum
                StrumEvent?.Invoke();
            }

            // Constantly activate starpower
            CallStarpowerEvent();
        }

        protected override void UpdateNavigationMode()
        {
            NavigationEventForMapping(MenuAction.Confirm, GREEN);
            NavigationEventForMapping(MenuAction.Back, RED);

            NavigationEventForMapping(MenuAction.Shortcut1, YELLOW);
            NavigationEventForMapping(MenuAction.Shortcut2, BLUE);
            NavigationHoldableForMapping(MenuAction.Shortcut3, ORANGE);

            NavigationHoldableForMapping(MenuAction.Up, UP);
            NavigationHoldableForMapping(MenuAction.Down, DOWN);

            NavigationEventForMapping(MenuAction.More, STAR_POWER);

            if (WasMappingPressed(PAUSE))
            {
                CallPauseEvent();
            }
        }

        public override Instrument[] GetAllowedInstruments()
        {
            return new Instrument[]
            {
                Instrument.GUITAR, Instrument.BASS, Instrument.KEYS, Instrument.GUITAR_COOP, Instrument.RHYTHM,
            };
        }

        public override string GetTrackPath()
        {
            return "Tracks/Keys";
        }
    }
}