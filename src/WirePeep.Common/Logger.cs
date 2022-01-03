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

		private const string LocalColumn = "Local";
		private const string FailureIdColumn = "FailureId";
		private const string UtcColumn = "Utc";
		private const string SincePreviousColumn = "SincePrevious";
		private const string CommentColumn = "Comment";
		private const string LengthColumn = "Length";
		private const string PeerGroupColumn = nameof(PeerGroup);

		private readonly HashSet<Entry> headers = new();
		private StreamWriter? writer;
		private int batchDepth;

		#endregion

		#region Constructors

		public Logger(string? fileName, bool isSimple)
		{
			this.FileName = fileName ?? string.Empty;
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
				this.TryAddHeader(
					Entry.Simple, PeerGroupColumn, "LocalStart", LengthColumn, "LocalEnd", SincePreviousColumn, CommentColumn, "UtcStart", "UtcEnd");
				TimeSpan? length = endedUtc != null ? ConvertUtility.RoundToSeconds(endedUtc.Value - startedUtc) : null;
				this.AddValues(peerGroupName, startedUtc.ToLocalTime(), length, endedUtc?.ToLocalTime(), sincePrevious, comment, startedUtc, endedUtc);
			}
		}

		public void AddLogStart(DateTime utcNow)
		{
			using (this.BeginBatch())
			{
				this.TryAddHeader(Entry.LogStart, LocalColumn, UtcColumn);
				this.AddValues(Entry.LogStart, utcNow.ToLocalTime(), utcNow);
			}
		}

		public void AddFailureStart(string peerGroupName, DateTime utcNow, int failureId, TimeSpan? sincePrevious)
		{
			using (this.BeginBatch())
			{
				this.TryAddHeader(Entry.FailureStart, LocalColumn, FailureIdColumn, PeerGroupColumn, SincePreviousColumn, UtcColumn);
				this.AddValues(Entry.FailureStart, utcNow.ToLocalTime(), failureId, peerGroupName, sincePrevious, utcNow);
			}
		}

		public void AddFailureComment(string peerGroupName, DateTime utcNow, int failureId, string comment)
		{
			using (this.BeginBatch())
			{
				this.TryAddHeader(Entry.FailureComment, LocalColumn, FailureIdColumn, PeerGroupColumn, CommentColumn, UtcColumn);
				this.AddValues(Entry.FailureComment, utcNow.ToLocalTime(), failureId, peerGroupName, comment, utcNow);
			}
		}

		public void AddFailureEnd(string peerGroupName, DateTime utcNow, int failureId, TimeSpan length)
		{
			using (this.BeginBatch())
			{
				this.TryAddHeader(Entry.FailureEnd, LocalColumn, FailureIdColumn, PeerGroupColumn, LengthColumn, UtcColumn);
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
					Entry.LogSummary, LocalColumn, PeerGroupColumn, "FailCount", "TotalFail", "PercentFail", "MinFail", "MaxFail", "AvgFail", UtcColumn);
				this.AddValues(
					Entry.LogSummary, utcNow.ToLocalTime(), peerGroupName, failCount, totalFailLength, percentFailTime, minFail, maxFail, averageFail, utcNow);
			}
		}

		public void AddLogEnd(DateTime utcNow, TimeSpan monitoredLength)
		{
			using (this.BeginBatch())
			{
				this.TryAddHeader(Entry.LogEnd, LocalColumn, "Monitored", UtcColumn);
				this.AddValues(Entry.LogEnd, utcNow.ToLocalTime(), monitoredLength, utcNow);
			}
		}

		public IDisposable BeginBatch()
		{
			if (this.batchDepth++ == 0 && !string.IsNullOrEmpty(this.FileName))
			{
				bool create = this.IsSimple && this.headers.Count == 0;
				this.writer = create ? File.CreateText(this.FileName) : File.AppendText(this.FileName);
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
					List<string> values = new(headerValues.Length);
					values.Add("//" + entry);
					values.AddRange(headerValues);
					headerValues = values.ToArray();
				}

				this.AddValues(headerValues);
			}
		}

		private void AddValues(params object?[] values)
		{
			// this.writer can be null if no log file name was provided (e.g., no log folder is configured).
			if (this.writer != null)
			{
				CsvUtility.WriteLine(this.writer, values);
			}
		}

		#endregion
	}
}
