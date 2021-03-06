﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.SDKs;
using System;
using System.Text;

namespace StardewValley.Menus
{
    public class TextBox : ITextBox
    {
        #region Vars
        SpriteFont _font;
        public SpriteFont Font
        {
            get
            {
                return _font;
            }
        }
        Color _textColor;
        public Color TextColor
        {
            get
            {
                return _textColor;
            }
        }
        protected int _X;
        public virtual int X
        {
            get
            {
                return _X;
            }
            set
            {
                _X = value;
                DrawOrigin.X = _X + 16f;
            }
        }
        protected int _Y;
        public virtual int Y
        {
            get
            {
                return _Y;
            }
            set
            {
                _Y = value;
                DrawOrigin.Y = _Y + 8f;
            }
        }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool PasswordBox { get; set; }

        public bool limitWidth = true;
        public string Text
        {
            get
            {
                return GetText();
            }
            set
            {
                SetText(value);
            }
        }
        bool _selected;
        public bool Selected
        {
            get
            {
                return _selected;
            }
            set
            {
                if (_selected != value)
                {
                    _selected = value;
                    if (!_selected)
                    {
                        if (Game1.keyboardDispatcher.Subscriber == this)
                            Game1.keyboardDispatcher.Subscriber = null;
                        _showKeyboard = false;
                        if (Program.sdk is SteamHelper && (Program.sdk as SteamHelper).active)
                        {
                            (Program.sdk as SteamHelper).CancelKeyboard();
                        }
                    }
                    else
                    {
                        Game1.keyboardDispatcher.Subscriber = this;
                        _showKeyboard = true;
                    }
                }
            }
        }
        public bool numbersOnly = false;
        public int textLimit = -1;

        protected Vector2 DrawOrigin = new Vector2(16f, 8f);
        protected Texture2D _textBoxTexture;
        protected Texture2D _caretTexture;

        //just for arrow key/mouse selection, dont use it to handle text change
        SelState selState = SelState.SEL_AE_NONE;
        bool _showKeyboard;
        StringBuilder text = new StringBuilder();
        #endregion Vars
        #region TextBox
        public delegate void TextBoxEvent(TextBox sender);

        public event TextBoxEvent OnEnterPressed;
        public event TextBoxEvent OnTabPressed;
        public event TextBoxEvent OnBackspacePressed;
        public TextBox(Texture2D textBoxTexture, Texture2D caretTexture, SpriteFont font, Color textColor)
        {
            _textBoxTexture = textBoxTexture;
            if (textBoxTexture != null)
            {
                Width = textBoxTexture.Width;
                Height = textBoxTexture.Height;
            }
            _caretTexture = caretTexture;
            _font = font;
            _textColor = textColor;
        }
        public void SelectMe()
        {
            Selected = true;
        }
        public void Update()
        {
            Game1.input.GetMouseState();
            Point mousePoint = new Point(Game1.getMouseX(), Game1.getMouseY());
            Rectangle position = new Rectangle(X, Y, Width, Height);
            if (position.Contains(mousePoint))
            {
                Selected = true;
            }
            else
            {
                Selected = false;
            }
            if (_showKeyboard)
            {
                if (Game1.options.gamepadControls && !Game1.lastCursorMotionWasMouse)
                {
                    Game1.showTextEntry(this);
                }
                _showKeyboard = false;
            }
        }
        public void Hover(int x, int y)
        {
            if (x > X && x < X + Width && y > Y && y < Y + Height)
            {
                Game1.SetFreeCursorDrag();
            }
        }
        #endregion TextBox
        #region ITextBox
        protected Acp acp = new Acp();

        public virtual Acp GetSelection()
        {
            return acp;
        }

        public SelState GetSelState()
        {
            switch (selState)
            {
                case SelState.SEL_AE_START:
                case SelState.SEL_AE_END:
                    if (acp.Start == acp.End)
                    {
                        selState = SelState.SEL_AE_NONE;
                    }
                    break;
                case SelState.SEL_AE_NONE:
                    if (acp.Start != acp.End)
                    {
                        selState = SelState.SEL_AE_START;
                    }
                    break;
                default:
                    break;
            }
            return selState;
        }

        public virtual void SetSelection(int acpStart, int acpEnd)
        {
            acp.Start = acpStart;
            acp.End = acpEnd;

            int len = GetTextLength();
            if (acp.Start > len || acp.End > len)//out of range
                acp.End = acp.Start = len;//reset caret to the tail
#if TSF
            if (Game1.keyboardDispatcher.Subscriber == this)
                Game1.tsf.onSelChange();
#endif
        }

        public void SetSelState(SelState state)
        {
            selState = state;
        }

        public virtual string GetText()
        {
            return text.ToString();
        }

        public virtual RECT GetTextExt(Acp _acp)
        {
            var len = GetTextLength();
            if (_acp.End > len)
                _acp.End = len;
            RECT rect = new RECT();

            string text = PasswordBox ? new string('*', GetTextLength()) : GetText();
            //acpend may smaller than acpstart
            var start = Math.Min(_acp.Start, _acp.End);
            var end = Math.Max(_acp.Start, _acp.End);

            rect.left += (int)(Font.MeasureString(text.Substring(0, start)).X + DrawOrigin.X);
            rect.top = (int)DrawOrigin.Y;

            var vec_text = Font.MeasureString(text.Substring(start, end - start));
            rect.right = rect.left + (int)vec_text.X;
            rect.bottom = rect.top + (int)vec_text.Y;

            return rect;
        }

        public virtual int GetTextLength()
        {
            return text.Length;
        }

        public virtual Acp QueryInsert(Acp _acp, uint cch)
        {
            return _acp;//always allow insert, composition str may longer than result str
        }

        public virtual void SetText(string str)
        {
            text.Clear();
            text.Append(str);
            acp.Start = acp.End = text.Length;
#if TSF
            if (Game1.keyboardDispatcher.Subscriber == this)//changed by other method, except IME
            {
                Game1.tsf.onTextChange();
                Game1.tsf.onSelChange();
            }
#endif
        }

        public virtual void ReplaceSelection(string _text)
        {
            if (acp.End != acp.Start)//it means delete and insert text
            {
                var start = Math.Min(acp.Start, acp.End);
                text.Remove(start, Math.Abs(acp.Start - acp.End));
                acp.Start = acp.End = start;
            }
            if (_text != "" && (textLimit == -1 || text.Length + _text.Length < textLimit) && (Font.MeasureString(_text).X + Font.MeasureString(text).X) < Width - 16)
            {
                text.Insert(acp.Start, _text);
                acp.End += _text.Length;
                //IME input dont play sound, english input sound is handled at IKeyboadSubscriber
                //Game1.playSound("cowboy_monsterhit");//TSF may replace some word, which will make the sound strange
            }
        }

        public virtual Acp GetAcpByRange(RECT rect)
        {
            Acp result = new Acp();
            var text = GetText();
            float width = DrawOrigin.X;
            if (rect.left <= X + Width && rect.top <= Y + Height && rect.right >= X && rect.bottom >= Y)//check if overlap textbox
            {
                if (rect.right <= width)
                {
                    result.Start = result.End = 0;
                }
                else if (rect.left >= Font.MeasureString(text).X + width)
                {
                    result.Start = result.End = GetTextLength();
                }
                else
                {
                    for (int i = 0; i < text.Length; i++)
                    {
                        var char_x = Font.MeasureString(text[i].ToString()).X;
                        width += char_x;
                        if (width > rect.left)
                        {
                            result.Start += ((width - char_x / 2) <= rect.left) ? 1 : 0;//divide char from middle, if selection is on the left part, we dont sel this word
                            result.End = result.Start;
                            result.End += (((width - char_x / 2) < rect.right) && ((width - char_x / 2) > rect.left)) ? 1 : 0;
                            if (width >= rect.right)
                            {
                                return result;
                            }
                            for (i++; i < text.Length; i++)
                            {
                                char_x = Font.MeasureString(text[i].ToString()).X;
                                width += char_x;
                                if (width > rect.right)
                                {
                                    result.End += ((width - char_x / 2) < rect.right) ? 1 : 0;//divide char from middle, if selection is on the left part, we dont sel this word
                                    return result;
                                }
                                result.End++;
                            }
                            break;
                        }
                        result.Start++;
                    }
                }
            }
            else
            {
                result.Start = result.End = -1;
            }
            return result;
        }

        public bool AllowIME
        {
            get
            {
                return !numbersOnly;
            }
        }
        #endregion ITextBox
        #region IKeyboardSubscriber
        public virtual void RecieveCommandInput(char command)//IME will handle key event first, so these method just for english input(if it is using IME, we need to notify TSF)
        {
            switch (command)
            {
                case '\b':
                    if (acp.End == acp.Start && acp.Start > 0)//if not, means it alradey have something selected, we just delete it
                    {
                        acp.Start--;//it selected nothing, reduce end to delete a char
                    }
                    if (acp.End != acp.Start)
                    {
                        ReplaceSelection("");
#if TSF
                        if (Game1.keyboardDispatcher.Subscriber == this)
                        {
                            Game1.tsf.onTextChange();//not changed by IME, should notify
                            Game1.tsf.onSelChange();
                        }

#endif
                        if (Game1.gameMode != 3)
                        {
                            Game1.playSound("tinyWhip");
                            return;
                        }
                    }
                    OnBackspacePressed?.Invoke(this);
                    break;
                case '\r':
                    OnEnterPressed?.Invoke(this);
                    break;
                case '\t':
                    OnTabPressed?.Invoke(this);
                    break;
                default:
                    break;
            }
        }

        public virtual void RecieveSpecialInput(Keys key)//IME will handle key event first, so these method just for english input(if it is using IME, we need to notify TSF)
        {
            var shiftPressed = (Game1.input.GetKeyboardState().IsKeyDown(Keys.LeftShift) || Game1.GetKeyboardState().IsKeyDown(Keys.RightShift));
            switch (key)
            {
                case Keys.Left:
                    if (acp.Start > 0 || acp.End > 0)
                    {
                        switch (GetSelState())
                        {
                            case SelState.SEL_AE_START:
                                if (shiftPressed)
                                {
                                    if (acp.End > 0)
                                        acp.End--;
                                    else
                                        return;
                                }
                                else
                                {
                                    acp.End = acp.Start;
                                }
                                break;
                            case SelState.SEL_AE_END:
                                if (shiftPressed)
                                {
                                    if (acp.Start > 0)
                                        acp.Start--;
                                    else
                                        return;
                                }
                                else
                                {
                                    acp.End = acp.Start;
                                }
                                break;
                            case SelState.SEL_AE_NONE:
                                if (shiftPressed)
                                {
                                    acp.Start--;
                                    selState = SelState.SEL_AE_END;
                                }
                                else
                                {
                                    acp.End = acp.Start = --acp.Start;
                                }
                                break;
                        }
                        goto HasUpdated;
                    }
                    break;
                case Keys.Right:
                    var len = GetTextLength();
                    if (acp.Start < len || acp.End < len)
                    {
                        switch (GetSelState())
                        {
                            case SelState.SEL_AE_START:
                                if (shiftPressed)
                                {
                                    if (acp.End < len)
                                        acp.End++;
                                    else
                                        return;
                                }
                                else
                                {
                                    acp.Start = acp.End;
                                }
                                break;
                            case SelState.SEL_AE_END:
                                if (shiftPressed)
                                {
                                    if (acp.Start < len)
                                        acp.Start++;
                                    else
                                        return;
                                }
                                else
                                {
                                    acp.Start = acp.End;
                                }
                                break;
                            case SelState.SEL_AE_NONE:
                                if (shiftPressed)
                                {
                                    acp.End++;
                                    selState = SelState.SEL_AE_START;
                                }
                                else
                                {
                                    acp.End = acp.Start = ++acp.Start;
                                }
                                break;
                        }
                        goto HasUpdated;
                    }
                    break;
                default:
                    break;
            }
            return;
        HasUpdated:
            {
#if TSF
                if (Game1.keyboardDispatcher.Subscriber == this)
                    Game1.tsf.onSelChange();
#endif
            }
        }

        public virtual void RecieveTextInput(char inputChar)//IME will handle key event first, so these method just for english input(if it is using IME, we need to notify TSF)
        {
            RecieveTextInput(inputChar.ToString());
        }

        public virtual void RecieveTextInput(string text)//IME will handle key event first, so these method just for english input(if it is using IME, we need to notify TSF)
        {
            int dummy = -1;
            if (Selected && (!numbersOnly || int.TryParse(text, out dummy)) && (textLimit == -1 || Text.Length < textLimit))
            {
                if (Game1.gameMode != 3)
                    switch (text)
                    {
                        case "\"":
                            return;
                        case "$":
                            Game1.playSound("money");
                            break;
                        case "*":
                            Game1.playSound("hammer");
                            break;
                        case "+":
                            Game1.playSound("slimeHit");
                            break;
                        case "<":
                            Game1.playSound("crystal");
                            break;
                        case "=":
                            Game1.playSound("coin");
                            break;
                        default:
                            Game1.playSound("cowboy_monsterhit");
                            break;
                    }
                Acp temp_acp = new Acp();
                temp_acp.End = acp.End;
                temp_acp.Start = acp.Start;

                acp = QueryInsert(temp_acp, (uint)text.Length);
                ReplaceSelection(text);
#if TSF
                if (Game1.keyboardDispatcher.Subscriber == this)
                    Game1.tsf.onTextChange();//not changed by IME, should notify
#endif
                acp.Start = acp.End;
#if TSF
                if (Game1.keyboardDispatcher.Subscriber == this)
                    Game1.tsf.onSelChange();
#endif
            }
        }
        #endregion IKeyboardSubscriber
        #region Draw

        public virtual void Draw(SpriteBatch spriteBatch, bool drawShadow = true)
        {
            DrawBackGround(spriteBatch);

            float offset = DrawOrigin.X;
            if (Selected)
            {
                DrawByAcp(spriteBatch, new Acp(0, acp.Start), ref offset, TextColor, drawShadow);
                DrawCaret(spriteBatch, ref offset);
                DrawByAcp(spriteBatch, new Acp(acp.Start, GetTextLength()), ref offset, TextColor, drawShadow);
            }
            else
                DrawByAcp(spriteBatch, new Acp(0, GetTextLength()), ref offset, TextColor, drawShadow);

        }

        protected virtual void DrawByAcp(SpriteBatch spriteBatch, Acp acp, ref float offset, Color color, bool drawShadow = true)
        {
            var len = Math.Abs(acp.Start - acp.End);
            var start = Math.Min(acp.Start, acp.End);
            var _text = PasswordBox ? new string('*', len) : text.ToString(start, len);
            spriteBatch.DrawString(Font, _text, new Vector2(offset, DrawOrigin.Y), color);
            offset += Font.MeasureString(_text).X;
        }

        protected virtual void DrawCaret(SpriteBatch spriteBatch, ref float offset, bool drawShadow = true)
        {
            if (acp.End == acp.Start)
            {
                bool caretVisible = DateTime.UtcNow.Millisecond % 1000 >= 500;
                if (caretVisible)
                {
                    spriteBatch.Draw(Game1.staminaRect, new Rectangle((int)offset, (int)DrawOrigin.Y, 4, 32), TextColor);
                }
                offset += 4;
            }
            else
            {
                //Draw selection
                RECT rect = GetTextExt(acp);
                Texture2D selectionRect = new Texture2D(Game1.game1.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
                Color[] colors = new Color[1];
                selectionRect.GetData(colors);
                colors[0] = Color.Gray;
                colors[0].A = (byte)(0.6f * 255);
                selectionRect.SetData(colors);
                spriteBatch.Draw(selectionRect, new Rectangle(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top), Color.Gray);
            }
        }

        protected virtual void DrawBackGround(SpriteBatch spriteBatch)
        {
            if (_textBoxTexture != null)
            {
                spriteBatch.Draw(_textBoxTexture, new Rectangle(X, Y, 16, Height), new Rectangle?(new Rectangle(0, 0, 16, Height)), Color.White);
                spriteBatch.Draw(_textBoxTexture, new Rectangle(X + 16, Y, Width - 32, Height), new Rectangle?(new Rectangle(16, 0, 4, Height)), Color.White);
                spriteBatch.Draw(_textBoxTexture, new Rectangle(X + Width - 16, Y, 16, Height), new Rectangle?(new Rectangle(_textBoxTexture.Bounds.Width - 16, 0, 16, Height)), Color.White);
            }
            else
                Game1.drawDialogueBox(X - 32, Y - 112 + 10, Width + 80, Height, false, true, null, false, true, -1, -1, -1);
        }
        #endregion Draw
    }
}
