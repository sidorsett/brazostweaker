using System;
using System.Threading;
using System.Windows.Forms;

namespace BrazosTweaker
{
	/// <summary>
	/// CPU settings for all cores and a given P-state.
	/// </summary>
	public class PState
	{
		private static readonly int _numCores = System.Environment.ProcessorCount;
        private static int _maxPstate = K10Manager.GetHighestPState();

        private PStateMsr[] _msrs = new PStateMsr[_numCores];

		/// <summary>
		/// Gets the per-core settings.
		/// </summary>
		public PStateMsr[] Msrs { get { return _msrs; } }


		/// <summary>
		/// Private constructor. Use Load() or Decode() to create an instance.
		/// </summary>
		private PState()
		{
		}


		/// <summary>
		/// Loads the specified P-state from the cores' MSRs.
		/// </summary>
		/// <param name="index">Index of the hardware P-state (0-4) to be loaded.</param>
		public static PState Load(int index)
		{
			if (index < 0 || index > 4)
				throw new ArgumentOutOfRangeException("index");

			var r = new PState();

			for (int i = 0; i < _numCores; i++)
				r._msrs[i] = PStateMsr.Load(index, i);

			return r;
		}

		/// <summary>
		/// Saves the P-state to the cores' MSRs.
		/// </summary>
		/// <param name="index">Index of the hardware P-state (0-4) to be modified.</param>
		public void Save(int index)
		{
			if (index < 0 || index > 4)
				throw new ArgumentOutOfRangeException("index");

            if (index < 3) //dealing with CPU P-States
            {
                uint msrIndex = 0xC0010064u + (uint)index;

                //int boostedStates = K10Manager.GetNumBoostedStates();
                int boostedStates = 0;
                int indexSw = Math.Max(0, index - boostedStates);

                int tempPStateHw = (index <= boostedStates ? _maxPstate : 0);
                int tempPStateSw = Math.Max(0, tempPStateHw - boostedStates);

                // switch temporarily to the highest thread priority
                // (we try not to interfere with any kind of C&Q)
                var previousPriority = Thread.CurrentThread.Priority;
                Thread.CurrentThread.Priority = ThreadPriority.Highest;

                bool[] applyImmediately = new bool[_numCores];
                for (int i = 0; i < _numCores; i++)
                {
                    applyImmediately[i] = (K10Manager.GetCurrentPState(i) == index);

                    // if the settings are to be applied immediately, switch temporarily to another P-state
                    if (applyImmediately[i])
                        K10Manager.SwitchToPState(tempPStateSw, i);
                }
                Thread.Sleep(3); // let transitions complete
                for (int i = 0; i < _numCores; i++)
                {
                    // save the new settings
                    ulong msr = Program.Ols.ReadMsr(msrIndex, i);
                    const ulong mask = 0xFE00FFFFu;
                    msr = (msr & ~mask) | (_msrs[i].Encode(index) & mask);
                    Program.Ols.WriteMsr(msrIndex, msr, i);

                    // apply the new settings by switching back
                    if (applyImmediately[i])
                        K10Manager.SwitchToPState(indexSw, i);
                }
                Thread.Sleep(3); // let transitions complete

                Thread.CurrentThread.Priority = previousPriority;
            } else if (index == 3 || index == 4) //dealing with NB P-State 0
            {
                // switch temporarily to the highest thread priority
                // (we try not to interfere with any kind of C&Q)
                var previousPriority = Thread.CurrentThread.Priority;
                Thread.CurrentThread.Priority = ThreadPriority.Highest;

                uint voltage = Program.Ols.ReadPciConfig(0xC3, 0x15C);
                const uint maskvolt = 0x7F7F7F7F;
                const uint check = 0x7C7C7C7C;
                voltage = (voltage & ~maskvolt) | (check & maskvolt);
                Program.Ols.WritePciConfig(0xC3, 0x15C, voltage);

                //string message = "Start: " + curNbstate;

                //check, if current NB P-State is the one, which is going to be modified
                index = index - 3;
                if (index == 0) // NB P-state0
                {
                    // save the new settings
                    uint config = Program.Ols.ReadPciConfig(0xC3, 0xDC);
                    const uint mask = 0x0007F000; //enable overwrite of Vid only
                    config = (config & ~mask) | (_msrs[0].Encode(index + 3) & mask);
                    Program.Ols.WritePciConfig(0xC3, 0xDC, config);
                } else if (index == 1)
                {
                    // save the new settings
                    uint config = Program.Ols.ReadPciConfig(0xC6, 0x90);
                    const uint mask = 0x00007F00; //enable VID modification only
                    config = (config & ~mask) | (_msrs[0].Encode(index + 3) & mask);
                    Program.Ols.WritePciConfig(0xC6, 0x90, config);

                    int curNbstate = K10Manager.GetNbPState();
                    if (curNbstate == 1)
                    {
                        K10Manager.EnableNBPstateSwitching();
                        K10Manager.SwitchToNbPState(0);
                        K10Manager.DisableNBPstateSwitching();
                    }
		}

                //MessageBox.Show(message);
                Thread.CurrentThread.Priority = previousPriority;
            }
		}


		/// <summary>
		/// Decodes a P-state from its string representation.
		/// </summary>
		/// <returns></returns>
		public static PState Decode(string text,int pstate)
		{
			if (string.IsNullOrEmpty(text))
				return null;

			string[] tokens = text.Split(new char[1] { '|' }, StringSplitOptions.RemoveEmptyEntries);
			if (tokens == null || tokens.Length != _numCores)
				return null;

			var r = new PState();

			for (int i = 0; i < _numCores; i++)
			{
				uint value = uint.Parse(tokens[i], System.Globalization.NumberStyles.HexNumber);
				r._msrs[i] = PStateMsr.Decode(value,pstate);
			}

			return r;
		}

		/// <summary>
		/// Encodes the P-state into a string.
		/// </summary>
		public string Encode(int pstate)
		{
			var sb = new System.Text.StringBuilder();

			for (int i = 0; i < _numCores; i++)
			{
				uint value = _msrs[i].Encode(pstate);

				sb.Append(value.ToString("X8"));

				if (i < _numCores - 1)
					sb.Append('|');
			}

			return sb.ToString();
		}


		/// <summary>
		/// Returns a human-readable string representation.
		/// </summary>
		public override string ToString()
		{
			var sb = new System.Text.StringBuilder();

            double maxVid = 0, maxCLK = 0;
            int maxPLL = 0;
			for (int i = 0; i < _msrs.Length; i++)
			{
				sb.Append(_msrs[i].Divider);
				if (i < _numCores - 1)
					sb.Append('|');

                maxVid = Math.Max(maxVid, _msrs[i].Vid);
                maxCLK = Math.Max(maxCLK, _msrs[i].CLK);
                maxPLL = (int)Math.Max(maxCLK, _msrs[i].PLL);
			}

            sb.AppendFormat(" @ {0}V/{1}MHz/{2}MHz", maxVid, maxCLK, maxPLL);

			return sb.ToString();
		}
	}
}
