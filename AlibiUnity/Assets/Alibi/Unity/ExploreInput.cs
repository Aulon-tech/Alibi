using UnityEngine;

namespace Alibi.Unity
{
    public enum ExploreKey { Up, Down, Left, Right, Run, TurnLeft, TurnRight, Board, Close, Ask, Search, Passage, Q1, Q2, Q3, Q4, Q5, Q6, TestTalk, TestLook }

    public static class ExploreInput
    {
        public static bool Held(ExploreKey k)
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return false;
            switch (k)
            {
                case ExploreKey.Up: return kb.wKey.isPressed || kb.upArrowKey.isPressed;
                case ExploreKey.Down: return kb.sKey.isPressed || kb.downArrowKey.isPressed;
                case ExploreKey.Left: return kb.aKey.isPressed || kb.leftArrowKey.isPressed;
                case ExploreKey.Right: return kb.dKey.isPressed || kb.rightArrowKey.isPressed;
                case ExploreKey.Run: return kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
                case ExploreKey.TurnLeft: return kb.qKey.isPressed;
                case ExploreKey.TurnRight: return kb.eKey.isPressed;
                case ExploreKey.TestLook: return kb.lKey.isPressed;
            }
            return false;
#else
            switch (k)
            {
                case ExploreKey.Up: return Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
                case ExploreKey.Down: return Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
                case ExploreKey.Left: return Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
                case ExploreKey.Right: return Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
                case ExploreKey.Run: return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                case ExploreKey.TurnLeft: return Input.GetKey(KeyCode.Q);
                case ExploreKey.TurnRight: return Input.GetKey(KeyCode.E);
                case ExploreKey.TestLook: return Input.GetKey(KeyCode.L);
            }
            return false;
#endif
        }

        public static bool Pressed(ExploreKey k)
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return false;
            switch (k)
            {
                case ExploreKey.Board: return kb.tabKey.wasPressedThisFrame;
                case ExploreKey.Close: return kb.escapeKey.wasPressedThisFrame;
                case ExploreKey.Ask: return kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame;
                case ExploreKey.Search: return kb.kKey.wasPressedThisFrame;
                case ExploreKey.Passage: return kb.fKey.wasPressedThisFrame;
                case ExploreKey.Q1: return kb.digit1Key.wasPressedThisFrame;
                case ExploreKey.Q2: return kb.digit2Key.wasPressedThisFrame;
                case ExploreKey.Q3: return kb.digit3Key.wasPressedThisFrame;
                case ExploreKey.Q4: return kb.digit4Key.wasPressedThisFrame;
                case ExploreKey.Q5: return kb.digit5Key.wasPressedThisFrame;
                case ExploreKey.Q6: return kb.digit6Key.wasPressedThisFrame;
                case ExploreKey.TestTalk: return kb.tKey.wasPressedThisFrame;
            }
            return false;
#else
            switch (k)
            {
                case ExploreKey.Board: return Input.GetKeyDown(KeyCode.Tab);
                case ExploreKey.Close: return Input.GetKeyDown(KeyCode.Escape);
                case ExploreKey.Ask: return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
                case ExploreKey.Search: return Input.GetKeyDown(KeyCode.K);
                case ExploreKey.Passage: return Input.GetKeyDown(KeyCode.F);
                case ExploreKey.Q1: return Input.GetKeyDown(KeyCode.Alpha1);
                case ExploreKey.Q2: return Input.GetKeyDown(KeyCode.Alpha2);
                case ExploreKey.Q3: return Input.GetKeyDown(KeyCode.Alpha3);
                case ExploreKey.Q4: return Input.GetKeyDown(KeyCode.Alpha4);
                case ExploreKey.Q5: return Input.GetKeyDown(KeyCode.Alpha5);
                case ExploreKey.Q6: return Input.GetKeyDown(KeyCode.Alpha6);
                case ExploreKey.TestTalk: return Input.GetKeyDown(KeyCode.T);
            }
            return false;
#endif
        }
    }
}
