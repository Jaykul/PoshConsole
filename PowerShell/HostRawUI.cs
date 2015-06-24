using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Host;
using System.Text;

namespace PoshCode.PowerShell
{
	class HostRawUI :PSHostRawUserInterface
	{
		private ICSharpCode.AvalonEdit.TextEditor _control;

		public HostRawUI(ICSharpCode.AvalonEdit.TextEditor _control)
		{
			// TODO: Complete member initialization
			this._control = _control;
		}

		public override ConsoleColor BackgroundColor { get; set; }

		public override Size BufferSize 
		{
			get
			{
				return new Size(120,500);
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		public override Coordinates CursorPosition
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		public override int CursorSize
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		public override void FlushInputBuffer()
		{
			throw new NotImplementedException();
		}

		public override ConsoleColor ForegroundColor { get; set; }

		public override BufferCell[,] GetBufferContents(Rectangle rectangle)
		{
			throw new NotImplementedException();
		}

		public override bool KeyAvailable
		{
			get { throw new NotImplementedException(); }
		}

		public override Size MaxPhysicalWindowSize
		{
			get { throw new NotImplementedException(); }
		}

		public override Size MaxWindowSize
		{
			get { throw new NotImplementedException(); }
		}

		public override KeyInfo ReadKey(ReadKeyOptions options)
		{
			throw new NotImplementedException();
		}

		public override void ScrollBufferContents(Rectangle source, Coordinates destination, Rectangle clip, BufferCell fill)
		{
			throw new NotImplementedException();
		}

		public override void SetBufferContents(Rectangle rectangle, BufferCell fill)
		{
			throw new NotImplementedException();
		}

		public override void SetBufferContents(Coordinates origin, BufferCell[,] contents)
		{
			throw new NotImplementedException();
		}

		public override Coordinates WindowPosition
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		public override Size WindowSize
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		public override string WindowTitle
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}
	}
}
