using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DerouteSharp.Collab;

namespace DerouteSharp
{
	public partial class FormMain
	{
		private CollabSettings _collabSettings = new CollabSettings();
		private CollabClient _collabClient;
		private CoordinateThrottler _positionThrottler;
		private OfflineChangeQueue _offlineQueue;
		private Timer _collabStatusTimer;
		private int _collabUserCount = 0;
		private bool _isSyncing = false;
		private Dictionary<string, Color> _entityOriginalColors = new Dictionary<string, Color>();
		private Dictionary<string, string> _entityLockOwners = new Dictionary<string, string>();

		private void InitializeCollab()
		{
#if DEBUG && (!__MonoCS__)
			Console.WriteLine("[Collab] InitializeCollab: starting, enabled=" + _collabSettings.Enabled);
			Console.WriteLine("[Collab] InitializeCollab: serverUrl=" + _collabSettings.ServerUrl + ", sessionId=" + _collabSettings.SessionId + ", userId=" + _collabSettings.UserId);
#endif
			_collabClient = new CollabClient(_collabSettings);

			_collabClient.OnConnected += (s, e) =>
			{
#if DEBUG && (!__MonoCS__)
				Console.WriteLine("[Collab] OnConnected event fired");
#endif
				InvokeOnUiThread(() =>
				{
					UpdateCollabStatus("Connected", _collabUserCount);
					FlushOfflineChanges();
				});
			};

			_collabClient.OnDisconnected += (s, e) =>
			{
#if DEBUG && (!__MonoCS__)
				Console.WriteLine("[Collab] OnDisconnected event fired");
#endif
				InvokeOnUiThread(() =>
				{
					_collabUserCount = 0;
					UpdateCollabStatus("Disconnected", 0);
				});
			};

			_collabClient.OnUserJoined += async (s, userId) =>
			{
#if DEBUG && (!__MonoCS__)
				Console.WriteLine($"[Collab UI] OnUserJoined: userId={userId}");
#endif
				await InvokeOnUiThreadAsync(async () =>
				{
					var users = await _collabClient.GetConnectedUsersAsync();
					_collabUserCount = users.Count;
					var color = _collabClient.GetUserColor(userId);
					var msg = $"User {userId} joined (color: {color})";
					toolStripStatusLabel1.Text = msg;
				});
			};

			_collabClient.OnUserLeft += (s, userId) =>
			{
#if DEBUG && (!__MonoCS__)
				Console.WriteLine($"[Collab UI] OnUserLeft: userId={userId}");
#endif
				InvokeOnUiThread(() =>
				{
					_collabUserCount = Math.Max(0, _collabUserCount - 1);
					toolStripStatusLabel1.Text = $"User {userId} left";
				});
			};

			_collabClient.OnEntityCreated += (s, data) =>
			{
#if DEBUG && (!__MonoCS__)
				Console.WriteLine($"[Collab UI] OnEntityCreated: id={data.Id}, type={data.Type}, createdBy={data.CreatedBy}");
#endif
				InvokeOnUiThread(() => ApplyRemoteEntity(data));
			};

			_collabClient.OnEntityUpdated += (s, data) =>
			{
#if DEBUG && (!__MonoCS__)
				Console.WriteLine($"[Collab UI] OnEntityUpdated: id={data.Id}, points={data.Points?.Count ?? 0}");
#endif
				InvokeOnUiThread(() => ApplyRemoteUpdate(data));
			};

			_collabClient.OnEntityLocked += (s, lockData) =>
			{
#if DEBUG && (!__MonoCS__)
				Console.WriteLine($"[Collab UI] OnEntityLocked: primitiveId={lockData.PrimitiveId}, lockedBy={lockData.LockedBy}");
#endif
				InvokeOnUiThread(() => ApplyRemoteLock(lockData));
			};

			_collabClient.OnEntityUnlocked += (s, lockData) =>
			{
#if DEBUG && (!__MonoCS__)
				Console.WriteLine($"[Collab UI] OnEntityUnlocked: primitiveId={lockData.PrimitiveId}");
#endif
				InvokeOnUiThread(() => ApplyRemoteUnlock(lockData));
			};

			_collabClient.OnEntityDeleted += (s, data) =>
			{
#if DEBUG && (!__MonoCS__)
				Console.WriteLine($"[Collab UI] OnEntityDeleted: id={data.Id}");
#endif
				InvokeOnUiThread(() => ApplyRemoteDelete(data));
			};

			_collabClient.OnCanvasCleared += (s, e) =>
			{
#if DEBUG && (!__MonoCS__)
				Console.WriteLine("[Collab UI] OnCanvasCleared");
#endif
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
#if DEBUG && (!__MonoCS__)
				Console.WriteLine("[Collab UI] OnSnapshotReceived");
#endif
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
							if (state.ContainsKey("entities"))
							{
								var entityList = state["entities"] as System.Collections.Generic.List<object>;
								if (entityList != null)
								{
									foreach (var entityObj in entityList)
									{
										try
										{
											var entityDict = entityObj as System.Collections.Generic.Dictionary<string, object>;
											if (entityDict == null) continue;

											var data = EntityConverter.FromDict(entityDict);
											if (string.IsNullOrEmpty(data.Id)) continue;

											var entity = EntityConverter.ToEntity(data, _collabSettings.UserId);
											entityBox1.root.Children.Add(entity);

											if (!string.IsNullOrEmpty(data.ColorOverride))
											{
												try { _entityOriginalColors[data.Id] = ColorTranslator.FromHtml(data.ColorOverride); }
												catch { }
											}

											if (!string.IsNullOrEmpty(data.LockedBy) && data.LockedBy != "none" && data.LockedBy != _collabSettings.UserId)
											{
												_entityLockOwners[data.Id] = data.LockedBy;
												entity.ColorOverride = Color.FromArgb(150, ColorTranslator.FromHtml(_collabClient.GetUserColor(data.LockedBy)));
											}
										}
										catch (Exception ex)
										{
											Console.WriteLine($"Error applying snapshot entity: {ex.Message}");
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
					// Reconnection progress is a status change, not an error
					if (error != null && error.StartsWith("Connection lost. Reconnecting"))
					{
						UpdateCollabStatus("Connecting", _collabUserCount);
						SetStatusMessage(error);
						return;
					}
					UpdateCollabStatus("Error", _collabUserCount);
					SetStatusMessage(error ?? "Unknown error");
				});
			};

			_collabStatusTimer = new Timer();
			_collabStatusTimer.Interval = 5000;
			_collabStatusTimer.Tick += (s, e) =>
			{
				InvokeOnUiThread(() => RefreshCollabStatus());
			};
			_collabStatusTimer.Start();

			statusStrip1.MouseDown += StatusStripMouseDown;
			collabStatusLabel.MouseDown += StatusStripMouseDown;
			collabStatusIndicator.MouseDown += StatusStripMouseDown;
			collabStatusMessage.MouseDown += StatusStripMouseDown;

			_positionThrottler = new CoordinateThrottler(this, 33);
			_positionThrottler.OnFlush += (updates) =>
			{
				foreach (var update in updates)
				{
					_collabClient.SendPositionUpdateAsync(update.PrimitiveId, update.Points);
				}
			};

			_offlineQueue = new OfflineChangeQueue();

			entityBox1.OnEntityAdd += EntityBox_OnEntityAdd;
			entityBox1.OnEntityRemove += EntityBox_OnEntityRemove;

			if (_collabSettings.Enabled && !string.IsNullOrEmpty(_collabSettings.ApiKey))
			{
#if DEBUG && (!__MonoCS__)
				Console.WriteLine("[Collab] Auto-connect enabled, starting connection...");
#endif
				UpdateCollabStatus("Connecting", 0);
				Task.Run(async () =>
				{
					await _collabClient.ConnectAsync();
				});
			}
			else
			{
#if DEBUG && (!__MonoCS__)
				Console.WriteLine("[Collab] Auto-connect skipped: enabled=" + _collabSettings.Enabled + ", hasKey=" + !string.IsNullOrEmpty(_collabSettings.ApiKey));
#endif
				UpdateCollabStatus("Disabled", 0);
			}
		}

		private void UpdateCollabStatus(string status, int userCount)
		{
			string text;
			System.Drawing.Color color;

			if (status.Contains("Error"))
			{
				text = "Collab: Error";
				color = System.Drawing.Color.Red;
				SetStatusMessage(status.StartsWith("Error:") ? status.Substring(7) : status);
			}
			else if (status.Contains("Connected"))
			{
				text = userCount > 0 ? $"Collab: Connected ({userCount} users)" : "Collab: Connected";
				color = System.Drawing.Color.Green;
			}
			else if (status.Contains("Connecting"))
			{
				text = "Collab: Connecting...";
				color = System.Drawing.Color.Orange;
			}
			else if (status.Contains("Disconnected"))
			{
				text = "Collab: Disconnected";
				color = System.Drawing.Color.Red;
			}
			else
			{
				text = "Collab: Disabled";
				color = System.Drawing.Color.Gray;
			}

			Action apply = () =>
			{
				collabStatusIndicator.Text = text;
				collabStatusIndicator.ForeColor = color;
			};

			if (InvokeRequired)
				Invoke(apply);
			else
				apply();
		}

		private void SetStatusMessage(string message)
		{
			Action apply = () =>
			{
				collabStatusMessage.Text = message ?? "";
				collabStatusMessage.ToolTipText = message ?? "";
			};

			if (InvokeRequired)
				Invoke(apply);
			else
				apply();
		}

		private async void RefreshCollabStatus()
		{
			if (_collabClient == null || !_collabClient.IsConnected || string.IsNullOrEmpty(_collabSettings.SessionId))
				return;

			try
			{
				var users = await _collabClient.GetConnectedUsersAsync();
				_collabUserCount = users.Count;

				var sessionShort = _collabSettings.SessionId.Length > 8
					? _collabSettings.SessionId.Substring(0, 8) + "..."
					: _collabSettings.SessionId;

				if (_collabClient.IsConnected)
				{
					var newText = $"Collab: Connected ({_collabUserCount} users, session: {sessionShort})";
					Action apply = () =>
					{
						collabStatusIndicator.Text = newText;
						collabStatusIndicator.ForeColor = System.Drawing.Color.Green;
					};
					if (InvokeRequired)
						Invoke(apply);
					else
						apply();
				}
			}
			catch
			{
				// Ignore errors during status refresh
			}
		}

		private void StatusStripMouseDown(object sender, MouseEventArgs e)
		{
			// Right-click anywhere on the status strip (or on the collab items themselves)
			// opens the CollabMCP menu. ToolStripItem.MouseDown does not bubble to the
			// strip, so the same handler is attached to the collab items as well.
			if (e.Button == MouseButtons.Right)
			{
				collabStatusContextMenu.Show(Control.MousePosition);
			}
		}

		private void CollabConnectMenuItem_Click(object sender, EventArgs e)
		{
#if DEBUG && (!__MonoCS__)
			Console.WriteLine("[Collab UI] CollabConnectMenuItem_Click");
#endif
			if (_collabClient != null)
			{
				UpdateCollabStatus("Connecting", _collabUserCount);
				Task.Run(async () =>
				{
					await _collabClient.ConnectAsync();
				});
			}
		}

		private void CollabDisconnectMenuItem_Click(object sender, EventArgs e)
		{
#if DEBUG && (!__MonoCS__)
			Console.WriteLine("[Collab UI] CollabDisconnectMenuItem_Click");
#endif
			if (_collabClient != null)
			{
				Task.Run(async () =>
				{
					await _collabClient.DisconnectAsync();
				});
			}
		}

		private void CollabReconnectMenuItem_Click(object sender, EventArgs e)
		{
#if DEBUG && (!__MonoCS__)
			Console.WriteLine("[Collab UI] CollabReconnectMenuItem_Click");
#endif
			if (_collabClient != null)
			{
				UpdateCollabStatus("Connecting", _collabUserCount);
				Task.Run(async () =>
				{
					await _collabClient.ConnectAsync();
				});
			}
		}

		private void CollabSessionMenuItem_Click(object sender, EventArgs e)
		{
#if DEBUG && (!__MonoCS__)
			Console.WriteLine("[Collab UI] CollabSessionMenuItem_Click");
#endif
			using (var dlg = new FormCollabSession(_collabClient, _collabSettings))
			{
				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					// Apply the new session without restarting: reconnect to it.
					Task.Run(async () =>
					{
						await _collabClient.DisconnectAsync();
						UpdateCollabStatus("Connecting", 0);
						await _collabClient.ConnectAsync();
					});
				}
			}
		}

		private void CollabUsersMenuItem_Click(object sender, EventArgs e)
		{
#if DEBUG && (!__MonoCS__)
			Console.WriteLine("[Collab UI] CollabUsersMenuItem_Click");
#endif
			using (var dlg = new FormCollabUsers(_collabClient, _collabSettings))
			{
				dlg.ShowDialog(this);
			}
		}

		/// <summary>Finds an entity anywhere in the canvas tree by its collab id (fallback: Label).</summary>
		private static Entity FindEntity(List<Entity> nodes, string id)
		{
			if (string.IsNullOrEmpty(id)) return null;
			foreach (var node in nodes)
			{
				if (node.CollabId == id || (string.IsNullOrEmpty(node.CollabId) && node.Label == id))
					return node;
				var child = FindEntity(node.Children, id);
				if (child != null) return child;
			}
			return null;
		}

		private void ApplyRemoteEntity(EntityData data)
		{
			if (_isSyncing || string.IsNullOrEmpty(data.Id)) return;

#if DEBUG && (!__MonoCS__)
			Console.WriteLine($"[Collab Apply] ApplyRemoteEntity: id={data.Id}, type={data.Type}, createdBy={data.CreatedBy}");
#endif
			var entity = EntityConverter.ToEntity(data, _collabSettings.UserId);
			entityBox1.root.Children.Add(entity);

			if (!string.IsNullOrEmpty(data.ColorOverride))
			{
				try { _entityOriginalColors[data.Id] = ColorTranslator.FromHtml(data.ColorOverride); }
				catch { }
			}

			if (!string.IsNullOrEmpty(data.LockedBy) && data.LockedBy != "none" && data.LockedBy != _collabSettings.UserId)
			{
				_entityLockOwners[data.Id] = data.LockedBy;
				entity.ColorOverride = Color.FromArgb(150, ColorTranslator.FromHtml(_collabClient.GetUserColor(data.LockedBy)));
			}

			entityBox1.Invalidate();
		}

		private void ApplyRemoteUpdate(EntityData data)
		{
			if (_isSyncing || string.IsNullOrEmpty(data.Id)) return;

#if DEBUG && (!__MonoCS__)
			Console.WriteLine($"[Collab Apply] ApplyRemoteUpdate: id={data.Id}, type={data.Type ?? "(position delta)"}, points={data.Points?.Count ?? 0}");
#endif
			var entity = FindEntity(entityBox1.root.Children, data.Id);
			if (entity == null)
			{
#if DEBUG && (!__MonoCS__)
				Console.WriteLine($"[Collab Apply] ApplyRemoteUpdate: entity not found for id={data.Id}");
#endif
				return;
			}

			if (string.IsNullOrEmpty(data.Type))
			{
				// Position delta (OnPositionUpdated): only coordinates change.
				if (data.Points != null && data.Points.Count >= 2)
				{
					entity.PathPoints = new List<System.Drawing.PointF>();
					for (int i = 0; i + 1 < data.Points.Count; i += 2)
						entity.PathPoints.Add(new System.Drawing.PointF(data.Points[i], data.Points[i + 1]));

					entity.LambdaX = data.Points[0];
					entity.LambdaY = data.Points[1];
					entity.LambdaEndX = data.Points[data.Points.Count - 2];
					entity.LambdaEndY = data.Points[data.Points.Count - 1];
				}
			}
			else
			{
				// Full entity update: apply all fields.
				var converted = EntityConverter.ToEntity(data, _collabSettings.UserId);
				EntityConverter.CopyTo(converted, entity);

				if (!string.IsNullOrEmpty(data.ColorOverride))
				{
					try { _entityOriginalColors[data.Id] = ColorTranslator.FromHtml(data.ColorOverride); }
					catch { }
				}

				if (_entityLockOwners.TryGetValue(data.Id, out var owner) && owner != _collabSettings.UserId)
				{
					entity.ColorOverride = Color.FromArgb(150, ColorTranslator.FromHtml(_collabClient.GetUserColor(owner)));
				}
			}

			entityBox1.Invalidate();
		}

		private void ApplyRemoteLock(LockData data)
		{
#if DEBUG && (!__MonoCS__)
			Console.WriteLine($"[Collab Apply] ApplyRemoteLock: primitiveId={data.PrimitiveId}, lockedBy={data.LockedBy}, isLocked={data.IsLocked}");
#endif
			var entity = FindEntity(entityBox1.root.Children, data.PrimitiveId);
			if (entity != null)
			{
				if (data.IsLocked && data.LockedBy != _collabSettings.UserId)
				{
					_entityLockOwners[data.PrimitiveId] = data.LockedBy;
					var lockColor = ColorTranslator.FromHtml(_collabClient.GetUserColor(data.LockedBy));
					entity.ColorOverride = Color.FromArgb(150, lockColor);

					toolStripStatusLabel1.Text = $"Entity {data.PrimitiveId.Substring(0, Math.Min(8, data.PrimitiveId.Length))} locked by {data.LockedBy}";
				}
				entityBox1.Invalidate();
			}
		}

		private void ApplyRemoteUnlock(LockData data)
		{
#if DEBUG && (!__MonoCS__)
			Console.WriteLine($"[Collab Apply] ApplyRemoteUnlock: primitiveId={data.PrimitiveId}");
#endif
			var entity = FindEntity(entityBox1.root.Children, data.PrimitiveId);
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

		private void ApplyRemoteDelete(EntityData data)
		{
			if (string.IsNullOrEmpty(data.Id)) return;

#if DEBUG && (!__MonoCS__)
			Console.WriteLine($"[Collab Apply] ApplyRemoteDelete: id={data.Id}");
#endif
			var entity = FindEntity(entityBox1.root.Children, data.Id);
			if (entity != null)
			{
				// remove the subtree and clean up lock/color bookkeeping recursively
				RemoveEntityAndChildren(entityBox1.root.Children, entity);
				ClearBookkeeping(entity, data.Id);
				entityBox1.Invalidate();
			}
			else
			{
#if DEBUG && (!__MonoCS__)
				Console.WriteLine($"[Collab Apply] ApplyRemoteDelete: entity not found for id={data.Id}");
#endif
			}
		}

		private static bool RemoveEntityAndChildren(List<Entity> nodes, Entity target)
		{
			for (int i = 0; i < nodes.Count; i++)
			{
				if (nodes[i] == target)
				{
					nodes.RemoveAt(i);
					return true;
				}
				if (RemoveEntityAndChildren(nodes[i].Children, target))
					return true;
			}
			return false;
		}

		private void ClearBookkeeping(Entity entity, string id)
		{
			_entityOriginalColors.Remove(id);
			_entityLockOwners.Remove(id);
			foreach (var child in entity.Children)
				ClearBookkeeping(child, child.CollabId ?? child.Label);
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

		/// <summary>Runs an async action on the UI thread and awaits its completion without
		/// blocking the calling thread (avoids deadlocks from .Result on the UI thread).</summary>
		private Task InvokeOnUiThreadAsync(Func<Task> action)
		{
			if (!InvokeRequired)
				return action();

			var tcs = new TaskCompletionSource<bool>();
			BeginInvoke(new Action(async () =>
			{
				try
				{
					await action();
					tcs.SetResult(true);
				}
				catch (Exception ex)
				{
					tcs.SetException(ex);
				}
			}));
			return tcs.Task;
		}

		private void QueueOfflineChange(OfflineChange change)
		{
#if DEBUG && (!__MonoCS__)
			Console.WriteLine($"[Collab Offline] QueueOfflineChange: type={change.ChangeType}, primitiveId={change.PrimitiveId}");
#endif
			_offlineQueue.Add(change);
		}

		private async Task FlushOfflineChanges()
		{
			if (_offlineQueue == null || _offlineQueue.Count == 0 || _collabClient == null || !_collabClient.IsConnected)
				return;

#if DEBUG && (!__MonoCS__)
			Console.WriteLine($"[Collab Offline] FlushOfflineChanges: starting, queueCount={_offlineQueue.Count}, isConnected={_collabClient.IsConnected}");
#endif
			var changes = _offlineQueue.Flush();
#if DEBUG && (!__MonoCS__)
			Console.WriteLine($"[Collab Offline] FlushOfflineChanges: {changes.Count} changes to flush");
#endif
			foreach (var change in changes)
			{
#if DEBUG && (!__MonoCS__)
				Console.WriteLine($"[Collab Offline] Sending change: type={change.ChangeType}, primitiveId={change.PrimitiveId}");
#endif
				if (change.ChangeType == "created")
				{
					await _collabClient.SendEntityCreatedAsync(change.Entity, change.ParentId);
				}
				else if (change.ChangeType == "updated")
				{
					await _collabClient.SendEntityUpdatedAsync(change.Entity);
				}
				else if (change.ChangeType == "deleted")
				{
					await _collabClient.SendEntityDeletedAsync(change.PrimitiveId);
				}
			}

#if DEBUG && (!__MonoCS__)
			Console.WriteLine($"[Collab Offline] FlushOfflineChanges: {changes.Count} changes flushed");
#endif
		}

		private void EntityBox_OnEntityAdd(object sender, Entity entity, EventArgs e)
		{
			if (_collabClient == null || !_collabSettings.Enabled)
				return;

#if DEBUG && (!__MonoCS__)
			Console.WriteLine($"[Collab] EntityBox_OnEntityAdd: entityType={entity.Type}, label={entity.Label}, isConnected={_collabClient.IsConnected}");
#endif
			var data = EntityConverter.ToEntityData(entity, _collabSettings.UserId);
			entity.CollabId = data.Id; // the server entity id now identifies this entity
			var parentId = entity.parent != null && !string.IsNullOrEmpty(entity.parent.CollabId)
				? entity.parent.CollabId
				: "";

			if (_collabClient.IsConnected)
			{
				// Live sync: publish the new entity immediately.
				Task.Run(async () => await _collabClient.SendEntityCreatedAsync(data, parentId));
				return;
			}

			var change = new OfflineChange
			{
				ChangeType = "created",
				PrimitiveId = data.Id,
				ParentId = parentId,
				SessionId = _collabSettings.SessionId,
				Entity = data
			};

			InvokeOnUiThread(() =>
			{
				QueueOfflineChange(change);
			});
		}

		private void EntityBox_OnEntityRemove(object sender, Entity entity, EventArgs e)
		{
			if (_collabClient == null || !_collabSettings.Enabled)
				return;

#if DEBUG && (!__MonoCS__)
			Console.WriteLine($"[Collab] EntityBox_OnEntityRemove: entityType={entity.Type}, label={entity.Label}, isConnected={_collabClient.IsConnected}");
#endif
			var entityId = entity.CollabId ?? entity.Label ?? Guid.NewGuid().ToString();

			if (_collabClient.IsConnected)
			{
				// Live sync: delete the entity on the server immediately.
				Task.Run(async () => await _collabClient.SendEntityDeletedAsync(entityId));
				return;
			}

			var change = new OfflineChange
			{
				ChangeType = "deleted",
				PrimitiveId = entityId,
				SessionId = _collabSettings.SessionId
			};

			InvokeOnUiThread(() =>
			{
				QueueOfflineChange(change);
			});
		}
	}
}
