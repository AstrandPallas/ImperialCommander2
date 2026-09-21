using System.Collections.Generic;

namespace Saga.Tracking
{
	/// <summary>
	/// Step backwards through tracker edits made at the table.
	/// </summary>
	/// <remarks>
	/// Every control on the tracker panel is a single tap that changes real
	/// state -- a damage pip, a condition chip, a figure dying -- and the panel
	/// is used at arm's length, between turns, often by whoever is nearest. A
	/// mis-tap is routine, and without this the only way back is to re-enter
	/// the state by hand and hope you remembered it.
	///
	/// This records whole-tracker snapshots rather than inverse operations.
	/// Snapshots are a few hundred bytes, the capture already exists for
	/// saving, and unlike inverse operations they cannot drift out of step
	/// with the thing they are undoing: a new control added to the panel is
	/// undoable the moment it calls Record, with nothing else to write.
	/// </remarks>
	public sealed class UndoStack
	{
		/// <summary>One recorded step: what the tracker looked like, and what was about to change it.</summary>
		private sealed class Step
		{
			public TrackerStateData State;
			public string Description;
		}

		/// <summary>
		/// How many steps are kept.
		/// </summary>
		/// <remarks>
		/// Deep enough to cover a whole activation gone wrong, shallow enough
		/// that it is never a substitute for the save file.
		/// </remarks>
		public const int Depth = 40;

		private readonly TrackerManager _tracker;
		private readonly List<Step> _undo = new List<Step>();
		private readonly List<Step> _redo = new List<Step>();

		public UndoStack( TrackerManager tracker )
		{
			_tracker = tracker;
		}

		public int Count => _undo.Count;
		/// <summary>
		/// True when there is a step that would actually change something.
		/// </summary>
		/// <remarks>
		/// Steps recorded for a change that never happened are ignored here as
		/// well as in Undo, so the button is not offered when it would do
		/// nothing.
		/// </remarks>
		public bool CanUndo
		{
			get
			{
				if ( _tracker == null || _undo.Count == 0 ) return false;
				string here = TrackerManager.Fingerprint( _tracker.Capture() );
				foreach ( var step in _undo )
					if ( TrackerManager.Fingerprint( step.State ) != here ) return true;
				return false;
			}
		}
		public bool CanRedo => _redo.Count > 0;

		/// <summary>
		/// What undoing would take back, for the button's label.
		/// </summary>
		/// <remarks>
		/// Names the step Undo would actually reach, skipping the empty ones,
		/// so the button never promises something it will not do.
		/// </remarks>
		public string NextUndo
		{
			get
			{
				if ( _tracker == null || _undo.Count == 0 ) return null;
				string here = TrackerManager.Fingerprint( _tracker.Capture() );
				for ( int i = _undo.Count - 1; i >= 0; i-- )
					if ( TrackerManager.Fingerprint( _undo[i].State ) != here )
						return _undo[i].Description;
				return null;
			}
		}

		public string NextRedo => CanRedo ? _redo[_redo.Count - 1].Description : null;

		/// <summary>
		/// Record the state BEFORE a change, naming the change.
		/// </summary>
		/// <remarks>
		/// Called before mutating, so the snapshot is what Undo restores. A
		/// call that turns out to change nothing is dropped rather than
		/// leaving a step that appears to do nothing when taken -- pressing
		/// undo and watching the board not move is worse than no undo at all.
		/// </remarks>
		public void Record( string description )
		{
			if ( _tracker == null ) return;
			var state = _tracker.Capture();

			if ( _undo.Count > 0
				&& TrackerManager.Fingerprint( _undo[_undo.Count - 1].State )
					== TrackerManager.Fingerprint( state ) )
				return;

			_undo.Add( new Step { State = state, Description = description } );
			if ( _undo.Count > Depth ) _undo.RemoveAt( 0 );

			// A fresh edit ends the redo branch, as everywhere else.
			_redo.Clear();
		}

		/// <summary>Take back the last recorded change. Returns what was undone.</summary>
		public string Undo()
		{
			if ( _tracker == null ) return null;

			// Drop any steps that would restore the state we are already in.
			// Record runs BEFORE a change, so it cannot know whether one
			// actually followed -- a tap that opened a menu and closed it
			// again leaves a step behind. Deciding here is what guarantees
			// that pressing undo always visibly moves something.
			string here = TrackerManager.Fingerprint( _tracker.Capture() );
			while ( _undo.Count > 0
				&& TrackerManager.Fingerprint( _undo[_undo.Count - 1].State ) == here )
				_undo.RemoveAt( _undo.Count - 1 );

			if ( _undo.Count == 0 ) return null;

			var step = _undo[_undo.Count - 1];
			_undo.RemoveAt( _undo.Count - 1 );

			// Capture where we are first, so the undo itself can be redone.
			_redo.Add( new Step
			{
				State = _tracker.Capture(),
				Description = step.Description,
			} );

			_tracker.Restore( step.State );
			return step.Description;
		}

		/// <summary>Put back a change that was undone. Returns what was redone.</summary>
		public string Redo()
		{
			if ( !CanRedo || _tracker == null ) return null;

			var step = _redo[_redo.Count - 1];
			_redo.RemoveAt( _redo.Count - 1 );

			_undo.Add( new Step
			{
				State = _tracker.Capture(),
				Description = step.Description,
			} );

			_tracker.Restore( step.State );
			return step.Description;
		}

		/// <summary>Forget everything, for the start of a mission.</summary>
		public void Clear()
		{
			_undo.Clear();
			_redo.Clear();
		}
	}
}
