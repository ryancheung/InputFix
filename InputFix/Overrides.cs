﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Runtime.InteropServices;

namespace InputFix
{
    public class Overrides
    {
        /// <summary>
        /// really bad code， just make it work
        /// if you want a normal implementat， go to  StardewValleyFixDemo
        /// lazy, no more code review
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="hIMC"></param>
        /// <returns></returns>
        [DllImport("Imm32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr ImmReleaseContext(IntPtr hWnd, IntPtr hIMC);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("User32.dll", EntryPoint = "PostMessage")]
        public static extern int PostMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("User32.dll", EntryPoint = "PostMessage")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        internal static extern int MapWindowPoints(IntPtr hWndFrom, IntPtr hWndTo, ref RECT pt, int cPoints);

        private const int WM_IME_SETCONTEXT = 0x0281;

        private const int WM_IME_STARTCOMPOSITION = 0x010D;
        private const int WM_IME_COMPOSITION = 0x010F;
        private const int WM_IME_ENDCOMPOSITION = 0x010E;

        private const int TF_UNLOCKED = 0x060F;
        private const int TF_LOCKED = 0x0606;
        private const int TF_GETTEXTLENGTH = 0x060E;
        private const int TF_GETTEXT = 0x060D;
        private const int TF_CLEARTEXT = 0x060C;
        private const int TF_GETTEXTEXT = 0x060B;
        private const int TF_QUERYINSERT = 0x060A;

        private const int WM_SETTEXTBOX = 0x0610;
        private const int WM_TerminateComposition = 0x0611;

        private const int EM_REPLACESEL = 0x00C2;
        private const int EM_SETSEL = 0x00B1;
        private const int EM_GETSEL = 0x00B0;

        private const int WM_INPUTLANGCHANGE = 0x0051;

        private const int WM_KILLFOCUS = 0x008;

        private const int WM_KEYDOWN = 0x100;
        private const int WM_KEYUP = 0x101;
        private const int WM_CHAR = 0x102;

        private static bool notify = true;

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct ACP
        {
            public int acpStart;
            public int acpEnd;
        };

        public static bool KeyboardInput_HookProc(ref IntPtr __result, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, IntPtr ___prevWndProc, ref IntPtr ___hIMC)
        {
            //ModEntry.monitor.Log("MSG:" + msg, StardewModdingAPI.LogLevel.Trace);

            if (___hIMC != (IntPtr)0)
            {
                ImmReleaseContext(___prevWndProc, ___hIMC);
                ___hIMC = (IntPtr)0;
                ModEntry.monitor.Log("Released IMM Context", StardewModdingAPI.LogLevel.Trace);
            }
            try
            {
                switch (msg)
                {
                    //Key event
                    case WM_CHAR:
                        if (Game1.keyboardDispatcher.Subscriber != null)
                        {
                            var sub = Game1.keyboardDispatcher.Subscriber;
                            char ch = (char)wParam;
                            if (!char.IsControl(ch))
                            {
                                if (ModEntry.textbox_h._enable)
                                {
                                    ACP acp = new ACP();
                                    acp.acpEnd = ModEntry.textbox_h.ACP_End;
                                    acp.acpStart = ModEntry.textbox_h.ACP_Start;
                                    if (acp.acpEnd == acp.acpStart)
                                    {
                                        acp.acpEnd++;
                                    }
                                    IntPtr pacp = Marshal.AllocHGlobal(8);
                                    Marshal.StructureToPtr(acp, pacp, false);
                                    SendMessage(Game1.game1.Window.Handle, TF_QUERYINSERT, (int)pacp, 1);
                                    acp = (ACP)Marshal.PtrToStructure(pacp, typeof(ACP));
                                    Marshal.FreeHGlobal(pacp);

                                    if (acp.acpEnd != acp.acpStart)
                                    {
                                        ModEntry.textbox_h.ACP_End = acp.acpEnd;
                                        ModEntry.textbox_h.ACP_Start = acp.acpStart;
                                        notify = false;
                                        ReplaceSel(ch.ToString());
                                        notify = true;
                                        ModEntry.tsf.onTextChange();
                                        ModEntry.textbox_h.ACP_Start = ModEntry.textbox_h.ACP_End;
                                        ModEntry.tsf.onSelChange();
                                    }
                                }
                                else
                                    sub.RecieveTextInput(ch);
                            }
                            else if (ch == '\u0016')//paste
                            {
                                if (System.Windows.Forms.Clipboard.ContainsText())
                                {
                                    if (ModEntry.textbox_h._enable)
                                    {
                                        var text = System.Windows.Forms.Clipboard.GetText();
                                        ACP acp = new ACP();
                                        acp.acpEnd = ModEntry.textbox_h.ACP_End;
                                        acp.acpStart = ModEntry.textbox_h.ACP_Start;
                                        if (acp.acpEnd == acp.acpStart)
                                        {
                                            acp.acpEnd += text.Length;
                                        }
                                        IntPtr pacp = Marshal.AllocHGlobal(8);
                                        Marshal.StructureToPtr(acp, pacp, false);
                                        SendMessage(Game1.game1.Window.Handle, TF_QUERYINSERT, (int)pacp, 1);
                                        acp = (ACP)Marshal.PtrToStructure(pacp, typeof(ACP));
                                        Marshal.FreeHGlobal(pacp);

                                        if (acp.acpEnd != acp.acpStart)
                                        {
                                            ModEntry.textbox_h.ACP_End = acp.acpEnd;
                                            ModEntry.textbox_h.ACP_Start = acp.acpStart;
                                            notify = false;
                                            ReplaceSel(text);
                                            notify = true;
                                            ModEntry.tsf.onTextChange();
                                            ModEntry.textbox_h.ACP_Start = ModEntry.textbox_h.ACP_End;
                                            ModEntry.tsf.onSelChange();
                                        }
                                    }
                                    else
                                        sub.RecieveTextInput(System.Windows.Forms.Clipboard.GetText());
                                }
                            }
                            else
                            {
                                if (ModEntry.textbox_h._enable && ch == '\b')
                                {
                                    if (ModEntry.textbox_h.ACP_End == ModEntry.textbox_h.ACP_Start && ModEntry.textbox_h.ACP_End != 0)
                                    {
                                        ModEntry.textbox_h.ACP_End--;
                                    }
                                    if (ModEntry.textbox_h.ACP_End != ModEntry.textbox_h.ACP_Start)
                                    {
                                        notify = false;
                                        ReplaceSel("");
                                        notify = true;
                                        ModEntry.tsf.onTextChange();
                                        ModEntry.tsf.onSelChange();
                                    }
                                }
                                else
                                    sub.RecieveCommandInput(ch);
                            }
                        }
                        goto Handled;
                    case WM_KEYDOWN:
                        if (Game1.keyboardDispatcher.Subscriber != null)
                        {
                            var sub = Game1.keyboardDispatcher.Subscriber;
                            Keys key = (Keys)wParam;
                            if (key == Keys.Left && Math.Max(ModEntry.textbox_h.ACP_End, ModEntry.textbox_h.ACP_Start) > 0)
                            {
                                ModEntry.textbox_h.ACP_Start = ModEntry.textbox_h.ACP_End = Math.Max(0,
                                    ModEntry.textbox_h.ACP_End != ModEntry.textbox_h.ACP_Start ?
                                    Math.Min(ModEntry.textbox_h.ACP_End, ModEntry.textbox_h.ACP_Start) : ModEntry.textbox_h.ACP_End - 1);
                                ModEntry.tsf.onSelChange();
                                goto Handled;
                            }
                            if (key == Keys.Right)
                            {
                                ModEntry.textbox_h.ACP_Start = ModEntry.textbox_h.ACP_End = Math.Min(ModEntry.textbox_h.getTextLen(),
                                    ModEntry.textbox_h.ACP_End != ModEntry.textbox_h.ACP_Start ?
                                    Math.Max(ModEntry.textbox_h.ACP_End, ModEntry.textbox_h.ACP_Start) : ModEntry.textbox_h.ACP_End + 1);
                                ModEntry.tsf.onSelChange();
                                goto Handled;
                            }
                            notify = false;
                            sub.RecieveSpecialInput(key);
                            notify = true;
                        }
                        goto Handled;
                    case WM_KEYUP:
                        break;
                    //EM
                    case EM_GETSEL:
                        Marshal.WriteInt32(lParam, ModEntry.textbox_h.ACP_End);
                        Marshal.WriteInt32(wParam, ModEntry.textbox_h.ACP_Start);
                        ModEntry.monitor.Log("GETSEL ACP_Start:" + ModEntry.textbox_h.ACP_Start + "ACP_End:" + ModEntry.textbox_h.ACP_End, StardewModdingAPI.LogLevel.Trace);
                        goto Handled;
                    case EM_SETSEL:
                        ModEntry.textbox_h.ACP_End = (int)lParam;
                        ModEntry.textbox_h.ACP_Start = (int)wParam;

                        if (ModEntry.textbox_h.ACP_Start > ModEntry.textbox_h.getTextLen())
                            ModEntry.textbox_h.resetAcp();

                        ModEntry.monitor.Log("SETSEL ACP_Start:" + ModEntry.textbox_h.ACP_Start + "ACP_End:" + ModEntry.textbox_h.ACP_End, StardewModdingAPI.LogLevel.Trace);
                        goto Handled;
                    case EM_REPLACESEL:
                        notify = false;
                        ReplaceSel(Marshal.PtrToStringAuto(lParam));
                        notify = true;
                        goto Handled;
                    //TF
                    case TF_GETTEXTLENGTH:
                        if (ModEntry.textbox_h.current != null)
                            __result = (IntPtr)ModEntry.textbox_h.getTextLen();
                        goto Handled;
                    case TF_GETTEXT:
                        if (ModEntry.textbox_h.current != null)
                        {
                            var text = ModEntry.textbox_h.getText();
                            Marshal.Copy(text.ToCharArray(), 0, wParam, Math.Min(text.Length, (int)lParam));
                        }
                        goto Handled;
                    case TF_CLEARTEXT:
                        ModEntry.textbox_h.ACP_End = 0;
                        ModEntry.textbox_h.ACP_Start = 0;
                        goto Handled;
                    case TF_GETTEXTEXT:
                        if (ModEntry.textbox_h.current != null)
                        {
                            RECT prc = new RECT();
                            ACP acp = (ACP)Marshal.PtrToStructure(lParam, typeof(ACP));
                            prc.left = ModEntry.textbox_h.X + 18;
                            if (ModEntry.textbox_h.current is ChatTextBox)
                            {
                                prc.left -= 2;
                                int index = 0;
                                foreach (ChatSnippet item in ((ChatTextBox)ModEntry.textbox_h.current).finalText)
                                {
                                    index += item.emojiIndex != -1 ? 1 : item.message.Length;
                                    if (index >= acp.acpStart)
                                    {
                                        if (item.emojiIndex != -1)
                                        {
                                            prc.left += (int)item.myLength;
                                        }
                                        else
                                        {
                                            prc.left += (int)ModEntry.textbox_h.font.MeasureString(item.message.Substring(0, acp.acpStart - (index - item.message.Length))).X;
                                        }
                                        break;
                                    }
                                    prc.left += (int)item.myLength;
                                }
                            }
                            else
                            {
                                string text = ModEntry.textbox_h.getText();
                                prc.left += (int)ModEntry.textbox_h.font.MeasureString(text.Substring(0, acp.acpStart)).X;
                            }
                            prc.top = ModEntry.textbox_h.current.Y;

                            prc.right = prc.left + (int)ModEntry.textbox_h.font.MeasureString(" ").X * (acp.acpEnd - acp.acpStart);
                            prc.bottom = prc.top + (int)ModEntry.textbox_h.font.MeasureString(" ").Y + 8;

                            MapWindowPoints(Game1.game1.Window.Handle, (IntPtr)0, ref prc, 2);

                            Marshal.StructureToPtr(prc, wParam, false);//text ext
                            __result = (IntPtr)0;//clipped
                        }
                        goto Handled;
                    case TF_QUERYINSERT:
                        if (ModEntry.textbox_h.current != null && ModEntry.textbox_h.current.textLimit != -1)
                        {
                            var limit = ModEntry.textbox_h.current.textLimit - ModEntry.textbox_h.getTextLen();
                            ACP acp = (ACP)Marshal.PtrToStructure(wParam, typeof(ACP));
                            uint cch = (uint)lParam;

                            string str = String.Format("ACPTestStart:{0} ACPTestEnd:{1} cch:{2}", acp.acpStart, acp.acpStart, cch);
                            ModEntry.monitor.Log(str, StardewModdingAPI.LogLevel.Trace);

                            acp.acpStart = Math.Min(acp.acpStart, limit);
                            acp.acpEnd = Math.Min(acp.acpEnd, limit);

                            Marshal.StructureToPtr(acp, wParam, false);
                            string str_result = String.Format("ACPTestStart:{0} ACPTestEnd:{1}", acp.acpStart, acp.acpStart);
                            ModEntry.monitor.Log(str_result, StardewModdingAPI.LogLevel.Trace);
                        }
                        goto Handled;
                    //TerminateComposition
                    case WM_TerminateComposition:
                        ModEntry.tsf.TerminateComposition();
                        goto Handled;
                    //SetTextBox
                    case WM_SETTEXTBOX:
                        if (Game1.keyboardDispatcher.Subscriber != null && Game1.keyboardDispatcher.Subscriber is TextBox)
                        {
                            ModEntry.monitor.Log("Set Enable", StardewModdingAPI.LogLevel.Trace);
                            ModEntry.textbox_h.enableInput(true);
                            ModEntry.monitor.Log("Set TextBox", StardewModdingAPI.LogLevel.Trace);
                            ModEntry.textbox_h.SetTextBox((TextBox)Game1.keyboardDispatcher.Subscriber);
                        }
                        goto Handled;
                    //KillFocus
                    case WM_KILLFOCUS:
                        ModEntry.tsf.TerminateComposition();
                        break;
                    //IMEs
                    case WM_IME_STARTCOMPOSITION:
                        ModEntry.monitor.Log("StartComposition", StardewModdingAPI.LogLevel.Trace);
                        goto Handled;
                    case WM_IME_COMPOSITION:
                        ModEntry.monitor.Log("Composition", StardewModdingAPI.LogLevel.Trace);
                        goto Handled;
                    case WM_IME_ENDCOMPOSITION:
                        ModEntry.monitor.Log("EndComposition", StardewModdingAPI.LogLevel.Trace);
                        goto Handled;
                    case WM_IME_SETCONTEXT:
                    case WM_INPUTLANGCHANGE:
                        goto Handled;
                }
            }
            catch (Exception e)
            {
                if (ModEntry.textbox_h.current != null)
                    ModEntry.textbox_h.resetAcp();
                else
                    ModEntry.textbox_h.ACP_Start = ModEntry.textbox_h.ACP_End = 0;
                PostMessage(Game1.game1.Window.Handle, WM_TerminateComposition, 0, 0);
                ModEntry.textbox_h.enableInput(false);
                ModEntry.monitor.Log(e.Message, StardewModdingAPI.LogLevel.Error);
            }
            return true;
        Handled:
            return false;
        }
        public static void Subscriber_Set()
        {
            if (Game1.gameMode == Game1.playingGameMode || Game1.gameMode == Game1.titleScreenGameMode)//cant change input state except playing or in titlescreen,or the game will struck
            {
                PostMessage(Game1.game1.Window.Handle, WM_TerminateComposition, 0, 0);
                if (Game1.keyboardDispatcher.Subscriber is TextBox && !((TextBox)Game1.keyboardDispatcher.Subscriber).numbersOnly)
                {
                    ModEntry.textbox_h.enableInput(false);
                    PostMessage(Game1.game1.Window.Handle, WM_SETTEXTBOX, 0, 0);
                }
                else
                {
                    //ModEntry.monitor.Log("Set Disable", StardewModdingAPI.LogLevel.Trace);
                    ModEntry.textbox_h.enableInput(false);
                }
            }
        }
        public static void TextBox_Text(TextBox __instance)
        {
            if (__instance == ModEntry.textbox_h.current && notify)
            {
                ModEntry.textbox_h.resetAcp();
            }

        }

        public static void ReplaceSel(string replace)
        {
            if (ModEntry.textbox_h.current != null)
            {
                ModEntry.monitor.Log("ACP_Start:" + ModEntry.textbox_h.ACP_Start + "ACP_End:" + ModEntry.textbox_h.ACP_End, StardewModdingAPI.LogLevel.Trace);
                if (ModEntry.textbox_h.ACP_End < ModEntry.textbox_h.ACP_Start)
                {
                    var temp_acp = ModEntry.textbox_h.ACP_Start;
                    ModEntry.textbox_h.ACP_Start = ModEntry.textbox_h.ACP_End;
                    ModEntry.textbox_h.ACP_End = temp_acp;
                    try
                    {
                        if (ModEntry.textbox_h.current is ChatTextBox)
                        {
                            ChatTextBox chat = ModEntry.textbox_h.current as ChatTextBox;
                            int index = 0;
                            for (int i = 0; i < chat.finalText.Count && ModEntry.textbox_h.ACP_End - ModEntry.textbox_h.ACP_Start > 0; i++)
                            {
                                ChatSnippet item = chat.finalText[i];
                                index += item.emojiIndex != -1 ? 1 : item.message.Length;
                                if (index >= ModEntry.textbox_h.ACP_End)
                                {
                                    if (item.emojiIndex != -1)
                                    {
                                        chat.finalText.RemoveAt(i);
                                        i--;
                                        ModEntry.textbox_h.ACP_End--;
                                        index--;
                                        if (i >= 0 && chat.finalText.Count > i + 1 && chat.finalText[i].emojiIndex == -1 && chat.finalText[i + 1].emojiIndex == -1)
                                        {
                                            //both text,merge it
                                            chat.finalText[i].message += chat.finalText[i + 1].message;
                                            chat.finalText[i].myLength += chat.finalText[i + 1].myLength;
                                            chat.finalText.RemoveAt(i + 1);
                                        }
                                    }
                                    else
                                    {
                                        //acp selection may cross snippet, dont out of range
                                        var start = ModEntry.textbox_h.ACP_Start - (index - item.message.Length);
                                        int len = Math.Min(ModEntry.textbox_h.ACP_End - ModEntry.textbox_h.ACP_Start, item.message.Length - start);
                                        item.message = item.message.Remove(start, len);
                                        ModEntry.textbox_h.ACP_End -= len;
                                        index -= len;
                                        if (item.message.Length == 0)//empty, remove it
                                        {
                                            chat.finalText.RemoveAt(i);
                                            i--;
                                        }
                                        else
                                        {
                                            item.myLength = ModEntry.textbox_h.font.MeasureString(item.message).X;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            ModEntry.textbox_h.current.Text = ModEntry.textbox_h.current.Text.Remove(ModEntry.textbox_h.ACP_Start,
                            ModEntry.textbox_h.ACP_End - ModEntry.textbox_h.ACP_Start);

                            ModEntry.textbox_h.ACP_End = ModEntry.textbox_h.ACP_Start;
                        }
                        ModEntry.monitor.Log("After Remove ACP_Start:" + ModEntry.textbox_h.ACP_Start + "ACP_End:" + ModEntry.textbox_h.ACP_End, StardewModdingAPI.LogLevel.Trace);
                    }
                    catch (Exception)
                    {
                        ModEntry.textbox_h.resetAcp();
                        ModEntry.monitor.Log("Reset acp", StardewModdingAPI.LogLevel.Error);
                    }
                }
                if (ModEntry.textbox_h.current is ChatTextBox)
                {
                    ChatTextBox chat = ModEntry.textbox_h.current as ChatTextBox;
                    chat.updateWidth();
                    int index = 0;
                    ChatSnippet chatSnippet = new ChatSnippet(replace, LocalizedContentManager.CurrentLanguageCode);
                    if (chatSnippet.myLength + chat.currentWidth >= 830)
                    {
                        ModEntry.textbox_h.ACP_End = ModEntry.textbox_h.ACP_Start;
                        ModEntry.monitor.Log("Full ACP_Start:" + ModEntry.textbox_h.ACP_Start + "ACP_End:" + ModEntry.textbox_h.ACP_End, StardewModdingAPI.LogLevel.Trace);
                        return;
                    }
                    for (int i = 0; i < chat.finalText.Count; i++)
                    {
                        ChatSnippet item = chat.finalText[i];
                        index += item.emojiIndex != -1 ? 1 : item.message.Length;
                        if (index >= ModEntry.textbox_h.ACP_Start && item.emojiIndex == -1)//[text  [caret > ]   message][ = caret (index)]
                        {
                            item.message = item.message.Insert(ModEntry.textbox_h.ACP_Start - (index - item.message.Length), chatSnippet.message);
                            item.myLength += chatSnippet.myLength;
                            goto Final;
                        }
                        else if (index > ModEntry.textbox_h.ACP_Start)//[nothing/emoji][caret here][emoji(now index is here, larger than caret pos)]
                        {
                            chat.finalText.Insert(i, chatSnippet);
                            goto Final;
                        }
                    }
                    chat.finalText.Add(chatSnippet);
                Final:
                    ModEntry.textbox_h.ACP_End = ModEntry.textbox_h.ACP_Start + chatSnippet.message.Length;
                    chat.updateWidth();
                }
                else
                {
                    var temp = ModEntry.textbox_h.current.Text.Length;
                    ModEntry.textbox_h.current.Text = ModEntry.textbox_h.current.Text.Insert(ModEntry.textbox_h.ACP_Start, replace);
                    ModEntry.textbox_h.ACP_End = ModEntry.textbox_h.ACP_Start + ModEntry.textbox_h.current.Text.Length - temp;
                }
                ModEntry.monitor.Log("After Set ACP_Start:" + ModEntry.textbox_h.ACP_Start + "ACP_End:" + ModEntry.textbox_h.ACP_End, StardewModdingAPI.LogLevel.Trace);
            }
        }

        public static void HandleMouseEvents(object sender, UpdateTickedEventArgs e)
        {
            if (!ModEntry.textbox_h._enable || !(ModEntry._helper.Input.GetState(SButton.MouseLeft) == SButtonState.Pressed))
            {
                return;
            }
            ICursorPosition cursor = ModEntry._helper.Input.GetCursorPosition();
            bool ScreenPixels = (cursor.ScreenPixels.X > ModEntry.textbox_h.current.X
                && cursor.ScreenPixels.X < ModEntry.textbox_h.current.X + ModEntry.textbox_h.current.Width
                && cursor.ScreenPixels.Y > ModEntry.textbox_h.current.Y
                && cursor.ScreenPixels.Y < ModEntry.textbox_h.current.Y + ModEntry.textbox_h.current.Height);
            if (ScreenPixels)
            {
                float mouse_x = cursor.ScreenPixels.X;
                if (ModEntry.textbox_h.current is ChatTextBox)
                {
                    ChatTextBox chat = ModEntry.textbox_h.current as ChatTextBox;
                    float width = 12f + ModEntry.textbox_h.X;
                    if (mouse_x > 835)//emoji menu icon
                        return;
                    else if (mouse_x < width)
                        ModEntry.textbox_h.ACP_End = ModEntry.textbox_h.ACP_Start = 0;
                    else if (mouse_x > chat.currentWidth + width)
                    {
                        ModEntry.textbox_h.ACP_End = ModEntry.textbox_h.ACP_Start = ModEntry.textbox_h.getTextLen();
                    }
                    else
                    {
                        int acp = 0;
                        foreach (ChatSnippet item in chat.finalText)
                        {
                            width += item.myLength;
                            acp += item.emojiIndex != -1 ? 1 : item.message.Length;
                            if (width > mouse_x)
                            {
                                if (item.emojiIndex != -1)
                                {
                                    ModEntry.textbox_h.ACP_End = ModEntry.textbox_h.ACP_Start = acp + ((width - item.myLength / 2) > mouse_x ? -1 : 0);
                                }
                                else
                                {
                                    for (int i = item.message.Length - 1; i >= 0; i--)
                                    {
                                        var char_x = ModEntry.textbox_h.font.MeasureString(item.message[i].ToString()).X;
                                        width -= char_x;
                                        if (width < mouse_x)
                                        {
                                            ModEntry.textbox_h.ACP_End = ModEntry.textbox_h.ACP_Start = acp + ((width + char_x / 2) > mouse_x ? -1 : 0);
                                            break;
                                        }
                                        acp--;
                                    }
                                }
                                break;
                            }
                        }
                    }
                }
                else
                {
                    var text = ModEntry.textbox_h.getText();
                    float width = 16f + ModEntry.textbox_h.X;
                    if (mouse_x < width)
                        ModEntry.textbox_h.ACP_End = ModEntry.textbox_h.ACP_Start = 0;
                    else if (mouse_x > ModEntry.textbox_h.font.MeasureString(text).X + width)
                    {
                        ModEntry.textbox_h.ACP_End = ModEntry.textbox_h.ACP_Start = ModEntry.textbox_h.getTextLen();
                    }
                    else
                    {
                        int acp = 0;
                        for (int i = 0; i < text.Length; i++)
                        {
                            var char_x = ModEntry.textbox_h.font.MeasureString(text[i].ToString()).X;
                            width += char_x;
                            if (width > mouse_x)
                            {
                                ModEntry.textbox_h.ACP_End = ModEntry.textbox_h.ACP_Start = acp + ((width - char_x / 2) > mouse_x ? 0 : 1);
                                break;
                            }
                            acp++;
                        }
                    }
                }
                ModEntry.monitor.Log("LeftClick Set Acp:" + ModEntry.textbox_h.ACP_Start, LogLevel.Trace);
                ModEntry.tsf.onSelChange();
            }
        }

        public static bool Draw(TextBox __instance, SpriteBatch spriteBatch, Texture2D ____textBoxTexture, bool drawShadow = true)
        {
            if (!__instance.Selected || __instance != ModEntry.textbox_h.current)
                return true;
            try
            {
                bool caretVisible = DateTime.UtcNow.Millisecond % 1000 >= 500;
                //draw background
                if (____textBoxTexture != null)
                {
                    spriteBatch.Draw(____textBoxTexture, new Rectangle(__instance.X, __instance.Y, 16, __instance.Height), new Rectangle?(new Rectangle(0, 0, 16, __instance.Height)), Color.White);
                    spriteBatch.Draw(____textBoxTexture, new Rectangle(__instance.X + 16, __instance.Y, __instance.Width - 32, __instance.Height), new Rectangle?(new Rectangle(16, 0, 4, __instance.Height)), Color.White);
                    spriteBatch.Draw(____textBoxTexture, new Rectangle(__instance.X + __instance.Width - 16, __instance.Y, 16, __instance.Height), new Rectangle?(new Rectangle(____textBoxTexture.Bounds.Width - 16, 0, 16, __instance.Height)), Color.White);
                }
                else
                {
                    Game1.drawDialogueBox(__instance.X - 32, __instance.Y - 112 + 10, __instance.Width + 80, __instance.Height, false, true, null, false, true, -1, -1, -1);
                }
                //draw text
                if (__instance is ChatTextBox)
                {
                    ChatTextBox chat = __instance as ChatTextBox;

                    float xPositionSoFar = 12f;

                    int index = 0;
                    bool caretDrawed = false;
                    for (int i = 0; i < chat.finalText.Count; i++)
                    {
                        ChatSnippet item = chat.finalText[i];

                        index += item.emojiIndex != -1 ? 1 : item.message.Length;

                        if (index == ModEntry.textbox_h.ACP_Start && !caretDrawed)
                        {
                            if (item.emojiIndex != -1)
                            {
                                spriteBatch.Draw(ChatBox.emojiTexture,
                                    new Vector2(__instance.X + xPositionSoFar, __instance.Y + 12),
                                    new Rectangle?(new Rectangle(
                                        item.emojiIndex * 9 % ChatBox.emojiTexture.Width,
                                        item.emojiIndex * 9 / ChatBox.emojiTexture.Width * 9,
                                        9,
                                        9)),
                                    Color.White,
                                    0f,
                                    Vector2.Zero,
                                    4f,
                                    SpriteEffects.None,
                                    0.99f);
                                xPositionSoFar += item.myLength;
                            }
                            if (item.message != null)
                            {
                                spriteBatch.DrawString(ChatBox.messageFont(LocalizedContentManager.CurrentLanguageCode),
                                    item.message,
                                    new Vector2(__instance.X + xPositionSoFar, __instance.Y + 12),
                                    ChatMessage.getColorFromName(Game1.player.defaultChatColor),
                                    0f, Vector2.Zero,
                                    1f,
                                    SpriteEffects.None,
                                    0.99f);
                                xPositionSoFar += item.myLength;
                            }
                            if (caretVisible)
                            {
                                spriteBatch.Draw(Game1.staminaRect, new Rectangle((int)(xPositionSoFar), __instance.Y + 12, 4, 32), __instance.TextColor);
                            }
                            xPositionSoFar += 4;
                            caretDrawed = true;
                            continue;
                        }
                        else if (index > ModEntry.textbox_h.ACP_Start && !caretDrawed)//[text  [caret]  message]
                        {
                            if (item.message != null)
                            {
                                //seperate str
                                var sep_str1 = new ChatSnippet(item.message.Substring(0, ModEntry.textbox_h.ACP_Start - (index - item.message.Length)), LocalizedContentManager.CurrentLanguageCode);
                                var sep_str2 = new ChatSnippet(item.message.Substring(ModEntry.textbox_h.ACP_Start - (index - item.message.Length)), LocalizedContentManager.CurrentLanguageCode);
                                if (sep_str1.message != null)
                                {
                                    spriteBatch.DrawString(ChatBox.messageFont(LocalizedContentManager.CurrentLanguageCode),
                                        sep_str1.message,
                                        new Vector2(__instance.X + xPositionSoFar, __instance.Y + 12),
                                        ChatMessage.getColorFromName(Game1.player.defaultChatColor),
                                        0f, Vector2.Zero,
                                        1f,
                                        SpriteEffects.None,
                                        0.99f);
                                }
                                xPositionSoFar += sep_str1.myLength;

                                if (caretVisible)
                                {
                                    spriteBatch.Draw(Game1.staminaRect, new Rectangle((int)(xPositionSoFar), __instance.Y + 12, 4, 32), __instance.TextColor);
                                }
                                xPositionSoFar += 4;

                                if (sep_str2.message != null)
                                {
                                    spriteBatch.DrawString(ChatBox.messageFont(LocalizedContentManager.CurrentLanguageCode),
                                        sep_str2.message,
                                        new Vector2(__instance.X + xPositionSoFar, __instance.Y + 12),
                                        ChatMessage.getColorFromName(Game1.player.defaultChatColor),
                                        0f, Vector2.Zero,
                                        1f,
                                        SpriteEffects.None,
                                        0.99f);
                                }
                                xPositionSoFar += sep_str2.myLength;
                            }
                            else
                            {
                                if (caretVisible)
                                {
                                    spriteBatch.Draw(Game1.staminaRect, new Rectangle((int)(xPositionSoFar), __instance.Y + 12, 4, 32), __instance.TextColor);
                                }
                                xPositionSoFar += 4;
                                if (item.emojiIndex != -1)
                                {
                                    spriteBatch.Draw(ChatBox.emojiTexture,
                                        new Vector2(__instance.X + xPositionSoFar, __instance.Y + 12),
                                        new Rectangle?(new Rectangle(
                                            item.emojiIndex * 9 % ChatBox.emojiTexture.Width,
                                            item.emojiIndex * 9 / ChatBox.emojiTexture.Width * 9,
                                            9,
                                            9)),
                                        Color.White,
                                        0f,
                                        Vector2.Zero,
                                        4f,
                                        SpriteEffects.None,
                                        0.99f);
                                    xPositionSoFar += item.myLength;
                                }
                            }
                            caretDrawed = true;
                            continue;
                        }

                        if (item.emojiIndex != -1)
                        {
                            spriteBatch.Draw(ChatBox.emojiTexture,
                                new Vector2(__instance.X + xPositionSoFar, __instance.Y + 12),
                                new Rectangle?(new Rectangle(
                                    item.emojiIndex * 9 % ChatBox.emojiTexture.Width,
                                    item.emojiIndex * 9 / ChatBox.emojiTexture.Width * 9,
                                    9,
                                    9)),
                                Color.White,
                                0f,
                                Vector2.Zero,
                                4f,
                                SpriteEffects.None,
                                0.99f);
                            xPositionSoFar += item.myLength;
                        }
                        if (item.message != null)
                        {
                            spriteBatch.DrawString(ChatBox.messageFont(LocalizedContentManager.CurrentLanguageCode),
                                item.message,
                                new Vector2(__instance.X + xPositionSoFar, __instance.Y + 12),
                                ChatMessage.getColorFromName(Game1.player.defaultChatColor),
                                0f, Vector2.Zero,
                                1f,
                                SpriteEffects.None,
                                0.99f);
                            xPositionSoFar += item.myLength;
                        }
                    }
                    if (!caretDrawed && caretVisible)
                    {
                        spriteBatch.Draw(Game1.staminaRect, new Rectangle((int)(xPositionSoFar), __instance.Y + 12, 4, 32), __instance.TextColor);
                    }
                }
                else
                {
                    string toDraw = __instance.PasswordBox ? new string('*', __instance.Text.Length) : __instance.Text;

                    int offset = __instance.X + 16;

                    var sep_str1 = toDraw.Substring(0, ModEntry.textbox_h.ACP_Start);
                    var sep_str2 = toDraw.Substring(ModEntry.textbox_h.ACP_Start);
                    var sep1_len = __instance.Font.MeasureString(sep_str1).X;

                    if (caretVisible)
                    {
                        //caret width = 4
                        spriteBatch.Draw(Game1.staminaRect, new Rectangle(offset + (int)sep1_len, __instance.Y + 8, 4, 32), __instance.TextColor);
                    }
                    if (drawShadow)
                    {
                        Utility.drawTextWithShadow(spriteBatch, sep_str1, __instance.Font, new Vector2(offset, __instance.Y + ((____textBoxTexture != null) ? 12 : 8)), __instance.TextColor, 1f, -1f, -1, -1, 1f, 3);
                        Utility.drawTextWithShadow(spriteBatch, sep_str2, __instance.Font, new Vector2(offset + sep1_len + 4, __instance.Y + ((____textBoxTexture != null) ? 12 : 8)), __instance.TextColor, 1f, -1f, -1, -1, 1f, 3);
                    }
                    else
                    {
                        spriteBatch.DrawString(__instance.Font, sep_str1, new Vector2(offset, __instance.Y + ((____textBoxTexture != null) ? 12 : 8)), __instance.TextColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.99f);
                        spriteBatch.DrawString(__instance.Font, sep_str2, new Vector2(offset + sep1_len + 4, __instance.Y + ((____textBoxTexture != null) ? 12 : 8)), __instance.TextColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.99f);
                    }
                }
            }
            catch (Exception e)
            {
                ModEntry.monitor.Log("Failed when drawing TextBox", LogLevel.Error);
                ModEntry.monitor.Log("Message:" + e.Message + " Source:" + e.Source, LogLevel.Error);
                ModEntry.monitor.Log(e.StackTrace, LogLevel.Error);
                return true;
            }
            return false;
        }
        public static bool receiveEmoji(ChatTextBox __instance, int emoji)
        {
            if (__instance.currentWidth + 40f > 830)
            {
                return false;
            }
            int index = 0;
            ChatSnippet chatSnippet = new ChatSnippet(emoji);
            for (int i = 0; i < __instance.finalText.Count; i++)
            {
                ChatSnippet item = __instance.finalText[i];
                index += item.emojiIndex != -1 ? 1 : item.message.Length;
                if (index == ModEntry.textbox_h.ACP_Start)//[text message/emoji][caret] 
                {
                    __instance.finalText.Insert(i + 1, chatSnippet);
                    goto FinalEmoji;
                }
                else if (index > ModEntry.textbox_h.ACP_Start)//[text  [caret]   message]
                {
                    var sep_str1 = new ChatSnippet(item.message.Substring(0, ModEntry.textbox_h.ACP_Start - (index - item.message.Length)), LocalizedContentManager.CurrentLanguageCode);
                    var sep_str2 = new ChatSnippet(item.message.Substring(ModEntry.textbox_h.ACP_Start - (index - item.message.Length)), LocalizedContentManager.CurrentLanguageCode);
                    __instance.finalText[i] = sep_str1;
                    __instance.finalText.Insert(i + 1, chatSnippet);
                    __instance.finalText.Insert(i + 2, sep_str2);
                    goto FinalEmoji;
                }
            }
            __instance.finalText.Add(chatSnippet);
        FinalEmoji:
            __instance.updateWidth();
            ModEntry.textbox_h.ACP_Start++;
            ModEntry.textbox_h.ACP_End++;
            ModEntry.tsf.onTextChange();
            return false;
        }

        public static bool CommandChatTextBoxDrawStart(TextBox __instance, SpriteBatch spriteBatch, Texture2D ____textBoxTexture, bool drawShadow = true)
        {
            return Draw(__instance, spriteBatch, ____textBoxTexture, drawShadow);
        }

        public static bool CommandChatTextBoxOnArrow(TextBox __instance)
        {
            return false;
        }
    }
}
