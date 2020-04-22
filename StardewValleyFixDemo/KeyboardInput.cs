﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Runtime.InteropServices;

namespace StardewValley
{
	public delegate void KeyEventHandler(object sender, KeyEventArgs e);
	public delegate void CharEnteredHandler(object sender, CharacterEventArgs e);
	public static class KeyboardInput
	{
		public static event CharEnteredHandler CharEntered;

		public static event KeyEventHandler KeyDown;

		public static event KeyEventHandler KeyUp;

		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

		private const int TF_UNLOCKED = 0x060F;
		private const int TF_LOCKED = 0x0606;
		private const int TF_GETTEXTLENGTH = 0x060E;
		private const int TF_GETTEXT = 0x060D;
		private const int TF_CLEARTEXT = 0x060C;
		private const int TF_GETTEXTEXT = 0x060B;
		private const int TF_QUERYINSERT = 0x060A;

		private const int EM_REPLACESEL = 0x00C2;
		private const int EM_SETSEL = 0x00B1;
		private const int EM_GETSEL = 0x00B0;

		private const int WM_KILLFOCUS = 0x008;

		private const int WM_KEYDOWN = 0x100;
		private const int WM_KEYUP = 0x101;
		private const int WM_CHAR = 0x102;

		private const int DLGC_WANTALLKEYS = 4;
		private const int WM_GETDLGCODE = 135;
		private const int GWL_WNDPROC = -4;
		public static void Initialize(GameWindow window)
		{
			if (KeyboardInput.initialized)
			{
				throw new InvalidOperationException("KeyboardInput.Initialize can only be called once!");
			}
			KeyboardInput.hookProcDelegate = new KeyboardInput.WndProc(KeyboardInput.HookProc);
			KeyboardInput.prevWndProc = (IntPtr)KeyboardInput.SetWindowLong(window.Handle, GWL_WNDPROC, (int)Marshal.GetFunctionPointerForDelegate(KeyboardInput.hookProcDelegate));
			KeyboardInput.initialized = true;
		}

		private static IntPtr HookProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
		{
			IntPtr returnCode = IntPtr.Zero;
			switch (msg)
			{
				case WM_GETDLGCODE:
					returnCode = (IntPtr)DLGC_WANTALLKEYS;
					break;
				case WM_CHAR:
					CharEntered?.Invoke(null, new CharacterEventArgs((char)wParam, (int)lParam));
                    break;
				case WM_KEYDOWN:
					KeyDown?.Invoke(null, new KeyEventArgs((Keys)wParam));
					break;
				case WM_KEYUP:
					KeyUp?.Invoke(null, new KeyEventArgs((Keys)wParam));
					break;
#if TSF
				case EM_GETSEL:
					if(Game1.keyboardDispatcher.Subscriber is ITextBox)
					{
						ITextBox textBox = Game1.keyboardDispatcher.Subscriber as ITextBox;
						ACP acp = textBox.GetSelection();
						Marshal.WriteInt32(wParam, acp.acpStart);
						Marshal.WriteInt32(lParam, acp.acpEnd);
					}
					break;
				case EM_SETSEL:
					if (Game1.keyboardDispatcher.Subscriber is ITextBox)
					{
						ITextBox textBox = Game1.keyboardDispatcher.Subscriber as ITextBox;
						textBox.SetSelection((int)wParam, (int)lParam);
					}
					break;
				case EM_REPLACESEL:
					if (Game1.keyboardDispatcher.Subscriber is ITextBox)
					{
						ITextBox textBox = Game1.keyboardDispatcher.Subscriber as ITextBox;
						textBox.ReplaceSelection(Marshal.PtrToStringAuto(lParam));
					}
					break;
				case TF_GETTEXT:
					if (Game1.keyboardDispatcher.Subscriber is ITextBox)
					{
						ITextBox textBox = Game1.keyboardDispatcher.Subscriber as ITextBox;
						var text = textBox.GetText();
						Marshal.Copy(text.ToCharArray(), 0, wParam, Math.Min(text.Length, (int)lParam));
					}
					break;
				case TF_GETTEXTLENGTH:
					if (Game1.keyboardDispatcher.Subscriber is ITextBox)
					{
						ITextBox textBox = Game1.keyboardDispatcher.Subscriber as ITextBox;
						returnCode = (IntPtr)textBox.GetTextLength();
					}
					break;
				case TF_GETTEXTEXT:
					if (Game1.keyboardDispatcher.Subscriber is ITextBox)
					{
						ITextBox textBox = Game1.keyboardDispatcher.Subscriber as ITextBox;
						ACP acp = (ACP)Marshal.PtrToStructure(lParam, typeof(ACP));
						Marshal.StructureToPtr(textBox.GetTextExt(acp), wParam, false);//text ext

						returnCode = (IntPtr)0;//if the rect clipped
					}
					break;
				case TF_QUERYINSERT:
					if (Game1.keyboardDispatcher.Subscriber is ITextBox)
					{
						ITextBox textBox = Game1.keyboardDispatcher.Subscriber as ITextBox;
						ACP acp = (ACP)Marshal.PtrToStructure(wParam, typeof(ACP));
						textBox.QueryInsert(acp, (uint)lParam);
						Marshal.StructureToPtr(acp, wParam, false);
					}
					break;
				case WM_KILLFOCUS:
					Game1.tsf.TerminateComposition();
					break;
#endif
				default:
					returnCode = KeyboardInput.CallWindowProc(KeyboardInput.prevWndProc, hWnd, msg, wParam, lParam);
					break;
			}
			return returnCode;
		}

		private static bool initialized;

		private static IntPtr prevWndProc;

		private static KeyboardInput.WndProc hookProcDelegate;

		private delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
	}
}