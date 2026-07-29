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

			_collabClient.OnUserJoined += (s, userId) =>
			{
#if DEBUG && (!__MonoCS__)
				Console.WriteLine($"[Collab UI] OnUserJoined: userId={userId}");
#endif
				InvokeOnUiThread(() =>
				{
					_collabUserCount = _collabClient.GetConnectedUsersAsync().Result.Count;
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

			_collabClient.OnPrimitiveCreated += (s, data) =>
			{
#if DEBUG && (!__MonoCS__)
				Console.WriteLine($"[Collab UI] OnPrimitiveCreated: id={data.Id}, type={data.Type}, createdBy={data.CreatedBy}");
#endif
				InvokeOnUiThread(() => ApplyRemotePrimitive(data));
			};

			_collabClient.OnPrimitiveUpdated += (s, data) =>
			{
#if DEBUG && (!__MonoCS__)
				Console.WriteLine($"[Collab UI] OnPrimitiveUpdated: id={data.Id}, points={data.Points?.Count ?? 0}");
#endif
				InvokeOnUiThread(() => ApplyRemoteUpdate(data));
			};

			_collabClient.OnPrimitiveLocked += (s, lockData) =>
			{
#if DEBUG && (!__MonoCS__)
				Console.WriteLine($"[Collab UI] OnPrimitiveLocked: primitiveId={lockData.PrimitiveId}, lockedBy={lockData.LockedBy}");
#endif
				InvokeOnUiThread(() => ApplyRemoteLock(lockData));
			};

			_collabClient.OnPrimitiveUnlocked += (s, lockData) =>
			{
#if DEBUG && (!__MonoCS__)
				Console.WriteLine($"[Collab UI] OnPrimitiveUnlocked: primitiveId={lockData.PrimitiveId}");
#endif
				InvokeOnUiThread(() => ApplyRemoteUnlock(lockData));
			};

			_collabClient.OnPrimitiveDeleted += (s, data) =>
			{
#if DEBUG && (!__MonoCS__)
				Console.WriteLine($"[Collab UI] OnPrimitiveDeleted: id={data.Id}");
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
					UpdateCollabStatus($"Error: {error.Substring(0, Math.Min(30, error.Length))}", _collabUserCount);
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
			}
		}

		private void UpdateCollabStatus(string status, int userCount)
		{
			string comboBoxText;
			int selectedIndex = 0;

			if (status.Contains("Error"))
			{
				comboBoxText = $"Error: {status.Substring(6)}";
				selectedIndex = 4;
			}
			else if (status.Contains("Connected"))
			{
				if (userCount > 0)
				{
					comboBoxText = $"Connected ({userCount} users)";
				}
				else
				{
					comboBoxText = "Connected";
				}
				selectedIndex = 1;
			}
			else if (status.Contains("Disconnected"))
			{
				comboBoxText = "Disconnected";
				selectedIndex = 3;
			}
			else if (status.Contains("Connecting"))
			{
				comboBoxText = "Connecting...";
				selectedIndex = 2;
			}
			else
			{
				comboBoxText = "Disabled";
				selectedIndex = 0;
			}

			if (InvokeRequired)
			{
				Invoke(new Action(() =>
				{
					collabStatusComboBox.SelectedIndex = selectedIndex;
					collabStatusComboBox.Text = comboBoxText;
				}));
			}
			else
			{
				collabStatusComboBox.SelectedIndex = selectedIndex;
				collabStatusComboBox.Text = comboBoxText;
			}
		}

		private async void RefreshCollabStatus()
		{
			if (!_collabClient.IsConnected || string.IsNullOrEmpty(_collabSettings.SessionId))
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
					var newText = $"Connected ({_collabUserCount} users, session: {sessionShort})";
					if (InvokeRequired)
					{
						Invoke(new Action(() =>
						{
							collabStatusComboBox.SelectedIndex = 1;
							collabStatusComboBox.Text = newText;
						}));
					}
					else
					{
						collabStatusComboBox.SelectedIndex = 1;
						collabStatusComboBox.Text = newText;
					}
				}
			}
			catch
			{
				// Ignore errors during status refresh
			}
		}

		private void StatusStripMouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right && e.X > 0 && e.X < statusStrip1.Items.Count * 100)
			{
				collabStatusContextMenu.Show(statusStrip1, e.Location);
			}
		}

		private void CollabReconnectMenuItem_Click(object sender, EventArgs e)
		{
#if DEBUG && (!__MonoCS__)
			Console.WriteLine("[Collab UI] CollabReconnectMenuItem_Click");
#endif
			if (_collabClient != null)
			{
				Task.Run(async () =>
				{
					await _collabClient.ConnectAsync();
				});
			}
		}

		private void CollabStatusComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
#if DEBUG && (!__MonoCS__)
			Console.WriteLine($"[Collab UI] CollabStatusComboBox_SelectedIndexChanged: {collabStatusComboBox.SelectedItem}");
#endif
			if (collabStatusComboBox.SelectedItem != null && collabStatusComboBox.SelectedItem.ToString() == "Reconnect")
			{
				if (_collabClient != null)
				{
					Task.Run(async () =>
					{
						await _collabClient.ConnectAsync();
					});
				}
			}
		}

		private void ApplyRemotePrimitive(VectorPrimitiveData data)
		{
			if (_isSyncing) return;

#if DEBUG && (!__MonoCS__)
			Console.WriteLine($"[Collab Apply] ApplyRemotePrimitive: id={data.Id}, type={data.Type}, color={data.StrokeColor}, createdBy={data.CreatedBy}, lockedBy={data.LockedBy}");
#endif
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

#if DEBUG && (!__MonoCS__)
			Console.WriteLine($"[Collab Apply] ApplyRemoteUpdate: id={data.Id}, points={data.Points?.Count ?? 0}, color={data.StrokeColor}");
#endif
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
			else
			{
#if DEBUG && (!__MonoCS__)
				Console.WriteLine($"[Collab Apply] ApplyRemoteUpdate: entity not found for id={data.Id}");
#endif
			}
		}

		private void ApplyRemoteLock(LockData data)
		{
#if DEBUG && (!__MonoCS__)
			Console.WriteLine($"[Collab Apply] ApplyRemoteLock: primitiveId={data.PrimitiveId}, lockedBy={data.LockedBy}, isLocked={data.IsLocked}");
#endif
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
#if DEBUG && (!__MonoCS__)
			Console.WriteLine($"[Collab Apply] ApplyRemoteUnlock: primitiveId={data.PrimitiveId}");
#endif
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
#if DEBUG && (!__MonoCS__)
			Console.WriteLine($"[Collab Apply] ApplyRemoteDelete: id={data.Id}");
#endif
			var entity = entityBox1.root.Children.FirstOrDefault(e => e.Label == data.Id);
			if (entity != null)
			{
				entityBox1.root.Children.Remove(entity);
				_entityOriginalColors.Remove(data.Id);
				_entityLockOwners.Remove(data.Id);
				entityBox1.Invalidate();
			}
			else
			{
#if DEBUG && (!__MonoCS__)
				Console.WriteLine($"[Collab Apply] ApplyRemoteDelete: entity not found for id={data.Id}");
#endif
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
#if DEBUG && (!__MonoCS__)
			Console.WriteLine($"[Collab Offline] QueueOfflineChange: type={change.ChangeType}, primitiveId={change.PrimitiveId}, entityType={change.EntityType}");
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
					var entityType = !string.IsNullOrEmpty(change.EntityType)
						? change.EntityType.ToLower()
						: "polyline";

					string primitiveType;
					switch (entityType)
					{
						case "region":
						case "rectangle":
						case "polygon":
						case "ellipse":
							primitiveType = "rectangle";
							break;
						case "wireinterconnect":
						case "line":
						case "polyline":
							primitiveType = "polyline";
							break;
						default:
							primitiveType = "polyline";
							break;
					}

					await _collabClient.SendPrimitiveCreatedAsync(
						primitiveType,
						change.Points ?? new List<float>(),
						change.StrokeColor ?? "#000000",
						change.StrokeWidth,
						change.FillColor ?? "transparent");
				}
				else if (change.ChangeType == "updated")
				{
					await _collabClient.SendPrimitiveUpdatedAsync(
						change.PrimitiveId,
						change.Points ?? new List<float>(),
						change.StrokeColor ?? "#000000",
						change.StrokeWidth,
						change.FillColor ?? "transparent");
				}
			}
		}

		private void EntityBox_OnEntityAdd(object sender, Entity entity, EventArgs e)
		{
			if (_collabClient == null || _collabClient.IsConnected)
				return;

#if DEBUG && (!__MonoCS__)
			Console.WriteLine($"[Collab Offline] EntityBox_OnEntityAdd: entityType={entity.Type}, label={entity.Label}, isConnected={_collabClient.IsConnected}");
#endif
			var primData = EntityConverter.ToPrimitiveData(entity, _collabSettings.UserId);

			var change = new OfflineChange
			{
				ChangeType = "created",
				PrimitiveId = primData.Id,
				SessionId = _collabSettings.SessionId,
				EntityType = entity.Type.ToString(),
				EntityLabel = entity.Label,
				Points = primData.Points,
				StrokeColor = primData.StrokeColor,
				StrokeWidth = primData.StrokeWidth,
				FillColor = primData.FillColor ?? "transparent",
				LambdaX = entity.LambdaX,
				LambdaY = entity.LambdaY,
				LambdaEndX = entity.LambdaEndX,
				LambdaEndY = entity.LambdaEndY,
				PathPoints = entity.PathPoints != null
					? entity.PathPoints.Select(p => (float)p.X).ToList()
					: null
			};

			InvokeOnUiThread(() =>
			{
				QueueOfflineChange(change);
			});
		}

		private void EntityBox_OnEntityRemove(object sender, Entity entity, EventArgs e)
		{
			if (_collabClient == null || _collabClient.IsConnected)
				return;

#if DEBUG && (!__MonoCS__)
			Console.WriteLine($"[Collab Offline] EntityBox_OnEntityRemove: entityType={entity.Type}, label={entity.Label}, isConnected={_collabClient.IsConnected}");
#endif
			var change = new OfflineChange
			{
				ChangeType = "deleted",
				PrimitiveId = entity.Label ?? Guid.NewGuid().ToString(),
				SessionId = _collabSettings.SessionId,
				EntityType = entity.Type.ToString(),
				EntityLabel = entity.Label
			};

			InvokeOnUiThread(() =>
			{
				QueueOfflineChange(change);
			});
		}
	}
}
