using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using DerouteSharp.Collab;
using DerouteSharp.Collab.UI;

namespace DerouteSharp
{
	public partial class FormMain
	{
		private CollabSettings _collabSettings = new CollabSettings();
		private CollabClient _collabClient;
		private CoordinateThrottler _positionThrottler;
		private OfflineChangeQueue _offlineQueue;
		private CollabStatusPanel _collabStatusPanel;
		private bool _isSyncing = false;
		private Dictionary<string, Color> _entityOriginalColors = new Dictionary<string, Color>();
		private Dictionary<string, string> _entityLockOwners = new Dictionary<string, string>();

		private void InitializeCollab()
		{
			_collabClient = new CollabClient(_collabSettings);

			_collabClient.OnConnected += (s, e) =>
			{
				InvokeOnUiThread(() =>
				{
					toolStripStatusLabel1.Text = "CollabMCP: Connected";
				});
			};

			_collabClient.OnDisconnected += (s, e) =>
			{
				InvokeOnUiThread(() =>
				{
					toolStripStatusLabel1.Text = "CollabMCP: Disconnected";
				});
			};

			_collabClient.OnUserJoined += (s, userId) =>
			{
				InvokeOnUiThread(() =>
				{
					var color = _collabClient.GetUserColor(userId);
					var msg = $"User {userId} joined (color: {color})";
					toolStripStatusLabel1.Text = msg;
				});
			};

			_collabClient.OnUserLeft += (s, userId) =>
			{
				InvokeOnUiThread(() =>
				{
					toolStripStatusLabel1.Text = $"User {userId} left";
				});
			};

			_collabClient.OnPrimitiveCreated += (s, data) =>
			{
				InvokeOnUiThread(() => ApplyRemotePrimitive(data));
			};

			_collabClient.OnPrimitiveUpdated += (s, data) =>
			{
				InvokeOnUiThread(() => ApplyRemoteUpdate(data));
			};

			_collabClient.OnPrimitiveLocked += (s, lockData) =>
			{
				InvokeOnUiThread(() => ApplyRemoteLock(lockData));
			};

			_collabClient.OnPrimitiveUnlocked += (s, lockData) =>
			{
				InvokeOnUiThread(() => ApplyRemoteUnlock(lockData));
			};

			_collabClient.OnPrimitiveDeleted += (s, data) =>
			{
				InvokeOnUiThread(() => ApplyRemoteDelete(data));
			};

			_collabClient.OnCanvasCleared += (s, e) =>
			{
				InvokeOnUiThread(() =>
				{
					if (entityBox1.root != null)
					{
						entityBox1.root.Children.Clear();
						entityBox1.Invalidate();
					}
					_entityOriginalColors.Clear();
					_entityLockOwners.Clear();
					toolStripStatusLabel1.Text = "Canvas cleared by collaborator";
				});
			};

			_collabClient.OnSnapshotReceived += (s, e) =>
			{
				InvokeOnUiThread(() =>
				{
					_isSyncing = true;
					entityBox1.root.Children.Clear();
					_entityOriginalColors.Clear();
					_entityLockOwners.Clear();

					Task.Run(async () =>
					{
						var state = await _collabClient.GetSessionStateAsync();
						InvokeOnUiThread(() =>
						{
							if (state.ContainsKey("primitives"))
							{
								var primList = state["primitives"] as System.Collections.Generic.List<object>;
								if (primList != null)
								{
									foreach (var primObj in primList)
									{
										try
										{
											var primDict = primObj as System.Collections.Generic.Dictionary<string, object>;
											if (primDict == null) continue;

											var primId = primDict["id"] as string;
											var primType = primDict["type"] as string;
											var points = primDict["points"] as System.Collections.Generic.List<object>;
											var strokeColor = primDict["strokeColor"] as string;
											var strokeWidth = Convert.ToSingle(primDict["strokeWidth"]);
											var createdBy = primDict["createdBy"] as string;
											var lockedBy = primDict["lockedBy"] as string;

											var color = ColorTranslator.FromHtml(strokeColor ?? "#000000");
											var entity = new Entity
											{
												Label = primId,
												Type = primType == "rectangle" ? EntityType.Region : EntityType.WireInterconnect,
												ColorOverride = color,
												WidthOverride = (int)strokeWidth,
												UserData = createdBy?.GetHashCode() ?? 0
											};

											if (points != null && points.Count >= 4)
											{
												entity.LambdaX = Convert.ToSingle(points[0]);
												entity.LambdaY = Convert.ToSingle(points[1]);
												entity.LambdaEndX = Convert.ToSingle(points[points.Count - 2]);
												entity.LambdaEndY = Convert.ToSingle(points[points.Count - 1]);
											}

											entityBox1.root.Children.Add(entity);
											_entityOriginalColors[primId] = color;

											if (!string.IsNullOrEmpty(lockedBy) && lockedBy != "none")
											{
												_entityLockOwners[primId] = lockedBy;
												entity.ColorOverride = Color.FromArgb(150, Color.Red);
											}
										}
										catch (Exception ex)
										{
											Console.WriteLine($"Error applying snapshot primitive: {ex.Message}");
										}
									}
								}
							}

							_isSyncing = false;
							entityBox1.Invalidate();
							toolStripStatusLabel1.Text = "CollabMCP: Snapshot applied";
						});
					});
				});
			};

			_collabClient.OnError += (s, error) =>
			{
				InvokeOnUiThread(() =>
				{
					toolStripStatusLabel1.Text = $"CollabMCP Error: {error}";
				});
			};

			_positionThrottler = new CoordinateThrottler(this, 33);
			_positionThrottler.OnFlush += (updates) =>
			{
				foreach (var update in updates)
				{
					_collabClient.SendPositionUpdateAsync(update.PrimitiveId, update.Points);
				}
			};

			_offlineQueue = new OfflineChangeQueue();

			if (_collabSettings.Enabled && !string.IsNullOrEmpty(_collabSettings.ApiKey))
			{
				Task.Run(async () =>
				{
					await _collabClient.ConnectAsync();
				});
			}

			_collabStatusPanel = new CollabStatusPanel(_collabClient, _collabSettings);
			this.Controls.Add(_collabStatusPanel);
			_collabStatusPanel.BringToFront();
		}

		private void ApplyRemotePrimitive(VectorPrimitiveData data)
		{
			if (_isSyncing) return;

			var color = ColorTranslator.FromHtml(data.StrokeColor ?? "#000000");
			var entity = EntityConverter.ToEntity(data, _collabSettings.UserId);

			entityBox1.root.Children.Add(entity);
			_entityOriginalColors[data.Id] = color;
			entityBox1.Invalidate();

			if (!string.IsNullOrEmpty(data.LockedBy) && data.LockedBy != "none" && data.LockedBy != _collabSettings.UserId)
			{
				_entityLockOwners[data.Id] = data.LockedBy;
				entity.ColorOverride = Color.FromArgb(150, Color.Red);
			}
		}

		private void ApplyRemoteUpdate(VectorPrimitiveData data)
		{
			if (_isSyncing) return;

			var entity = entityBox1.root.Children.FirstOrDefault(e => e.Label == data.Id);
			if (entity != null)
			{
				if (data.Points != null && data.Points.Count >= 4)
				{
					entity.LambdaX = data.Points[0];
					entity.LambdaY = data.Points[1];
					entity.LambdaEndX = data.Points[data.Points.Count - 2];
					entity.LambdaEndY = data.Points[data.Points.Count - 1];
				}

				if (!string.IsNullOrEmpty(data.StrokeColor))
				{
					var color = ColorTranslator.FromHtml(data.StrokeColor);
					_entityOriginalColors[data.Id] = color;

					if (_entityLockOwners.TryGetValue(data.Id, out var owner) && owner != _collabSettings.UserId)
					{
						entity.ColorOverride = Color.FromArgb(150, color);
					}
					else
					{
						entity.ColorOverride = color;
					}
				}

				entityBox1.Invalidate();
			}
		}

		private void ApplyRemoteLock(LockData data)
		{
			var entity = entityBox1.root.Children.FirstOrDefault(e => e.Label == data.PrimitiveId);
			if (entity != null)
			{
				if (data.IsLocked && data.LockedBy != _collabSettings.UserId)
				{
					_entityLockOwners[data.PrimitiveId] = data.LockedBy;
					var origColor = _entityOriginalColors.ContainsKey(data.PrimitiveId)
						? _entityOriginalColors[data.PrimitiveId]
						: Color.Black;
					entity.ColorOverride = Color.FromArgb(150, origColor);

					var lockColor = ColorTranslator.FromHtml(_collabClient.GetUserColor(data.LockedBy));
					entity.ColorOverride = Color.FromArgb(150, lockColor);

					toolStripStatusLabel1.Text = $"Entity {data.PrimitiveId.Substring(0, Math.Min(8, data.PrimitiveId.Length))} locked by {data.LockedBy}";
				}
				entityBox1.Invalidate();
			}
		}

		private void ApplyRemoteUnlock(LockData data)
		{
			var entity = entityBox1.root.Children.FirstOrDefault(e => e.Label == data.PrimitiveId);
			if (entity != null)
			{
				_entityLockOwners.Remove(data.PrimitiveId);
				if (_entityOriginalColors.TryGetValue(data.PrimitiveId, out var color))
				{
					entity.ColorOverride = color;
				}
				entityBox1.Invalidate();
			}
		}

		private void ApplyRemoteDelete(VectorPrimitiveData data)
		{
			var entity = entityBox1.root.Children.FirstOrDefault(e => e.Label == data.Id);
			if (entity != null)
			{
				entityBox1.root.Children.Remove(entity);
				_entityOriginalColors.Remove(data.Id);
				_entityLockOwners.Remove(data.Id);
				entityBox1.Invalidate();
			}
		}

		private void InvokeOnUiThread(Action action)
		{
			if (InvokeRequired)
			{
				Invoke(action);
			}
			else
			{
				action();
			}
		}

		private void QueueOfflineChange(OfflineChange change)
		{
			_offlineQueue.Add(change);
		}

		private async Task FlushOfflineChanges()
		{
			var changes = _offlineQueue.Flush();
			foreach (var change in changes)
			{
				if (change.Type == "created")
				{
					await _collabClient.SendPrimitiveCreatedAsync(
						change.Points != null ? "polyline" : "line",
						change.Points,
						change.StrokeColor,
						change.StrokeWidth,
						change.FillColor);
				}
				else if (change.Type == "updated")
				{
					await _collabClient.SendPrimitiveUpdatedAsync(
						change.PrimitiveId,
						change.Points,
						change.StrokeColor,
						change.StrokeWidth,
						change.FillColor);
				}
			}
		}
	}
}
