#region Using Directives

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Menees;

#endregion

namespace WirePeep
{
	public sealed class Logger
	{
		#region Private Data Members

		private readonly HashSet<Entry> headers = new HashSet<Entry>();
		private StreamWriter writer;
		private int batchDepth;

		#endregion

		#region Constructors

		public Logger(string fileName, bool isSimple)
		{
			Conditions.RequireString(fileName, nameof(fileName));

			this.FileName = fileName;
			this.IsSimple = isSimple;
		}

		#endregion

		#region Private Enums

		public enum Entry
		{
			Simple,
			LogStart,
			FailureStart,
			FailureComment,
			FailureEnd,
			LogSummary,
			LogEnd,
		}

		#endregion

		#region Public Properties

		public string FileName { get; }

		public bool IsSimple { get; }

		#endregion

		#region Public Methods

		public void AddSimpleEntry(string peerGroupName, DateTime startedUtc, DateTime? endedUtc, TimeSpan? sincePrevious, string comment)
		{
			using (this.BeginBatch())
			{
				this.TryAddHeader(Entry.Simple, nameof(PeerGroup), "LocalStart", "Length", "LocalEnd", "SincePrevious", "Comment", "UtcStart", "UtcEnd");
				TimeSpan? length = endedUtc != null ? ConvertUtility.RoundToSeconds(endedUtc.Value - startedUtc) : (TimeSpan?)null;
				this.AddValues(peerGroupName, startedUtc.ToLocalTime(), length, endedUtc?.ToLocalTime(), sincePrevious, comment, startedUtc, endedUtc);
			}
		}

		public void AddLogStart(DateTime utcNow)
		{
			using (this.BeginBatch())
			{
				this.TryAddHeader(Entry.LogStart, "Local", "Utc");
				this.AddValues(Entry.LogStart, utcNow.ToLocalTime(), utcNow);
			}
		}

		public void AddFailureStart(string peerGroupName, DateTime utcNow, int failureId, TimeSpan? sincePrevious)
		{
			using (this.BeginBatch())
			{
				this.TryAddHeader(Entry.FailureStart, "Local", "FailureId", nameof(PeerGroup), "SincePrevious", "Utc");
				this.AddValues(Entry.FailureStart, utcNow.ToLocalTime(), failureId, peerGroupName, sincePrevious, utcNow);
			}
		}

		public void AddFailureComment(string peerGroupName, DateTime utcNow, int failureId, string comment)
		{
			using (this.BeginBatch())
			{
				this.TryAddHeader(Entry.FailureComment, "Local", "FailureId", nameof(PeerGroup), "Comment", "Utc");
				this.AddValues(Entry.FailureComment, utcNow.ToLocalTime(), failureId, peerGroupName, comment, utcNow);
			}
		}

		public void AddFailureEnd(string peerGroupName, DateTime utcNow, int failureId, TimeSpan length)
		{
			using (this.BeginBatch())
			{
				this.TryAddHeader(Entry.FailureEnd, "Local", "FailureId", nameof(PeerGroup), "Length", "Utc");
				this.AddValues(Entry.FailureEnd, utcNow.ToLocalTime(), failureId, peerGroupName, length, utcNow);
			}
		}

		public void AddLogSummary(
			string peerGroupName,
			DateTime utcNow,
			int failCount,
			TimeSpan totalFailLength,
			decimal percentFailTime,
			TimeSpan minFail,
			TimeSpan maxFail,
			TimeSpan averageFail)
		{
			using (this.BeginBatch())
			{
				this.TryAddHeader(
					Entry.LogSummary, "Local", nameof(PeerGroup), "FailCount", "TotalFailLength", "PercentFailTime", "MinFail", "MaxFail", "AvgFail", "Utc");
				this.AddValues(
					Entry.LogSummary, utcNow.ToLocalTime(), peerGroupName, failCount, totalFailLength, percentFailTime, minFail, maxFail, averageFail, utcNow);
			}
		}

		public void AddLogEnd(DateTime utcNow, TimeSpan monitoredLength)
		{
			using (this.BeginBatch())
			{
				this.TryAddHeader(Entry.LogEnd, "Local", "MonitoredLength", "Utc");
				this.AddValues(Entry.LogEnd, utcNow.ToLocalTime(), monitoredLength, utcNow);
			}
		}

		public IDisposable BeginBatch()
		{
			if (this.batchDepth++ == 0)
			{
				this.writer = File.AppendText(this.FileName);
			}

			return new Disposer(() =>
			{
				if (--this.batchDepth == 0)
				{
					this.writer?.Dispose();
					this.writer = null;
				}
			});
		}

		#endregion

		#region Private Methods

		private void TryAddHeader(Entry entry, params string[] headerValues)
		{
			if (this.headers.Add(entry))
			{
				if (!this.IsSimple)
				{
					List<string> values = new List<string>(headerValues.Length);
					values.Add("//");
					values.AddRange(headerValues);
					headerValues = values.ToArray();
				}

				this.AddValues(headerValues);
			}
		}

		private void AddValues(params object[] values)
		{
			CsvUtility.WriteLine(this.writer, values);
		}

		#endregion
	}
}
