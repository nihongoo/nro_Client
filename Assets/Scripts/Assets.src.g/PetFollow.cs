namespace Assets.src.g
{
	public class PetFollow
	{
		public short smallID;

		public int dir;
		public int cmx;
		public int cmy;
		public int GroundFootInset { get; set; } = -1;

		private const int FlyingFollowDistance = 30;
		private const int GroundFollowDistance = 36;
		private const int GroundSpeed = 5;
		private const int CatchUpDistance = 144;
		private const int CloudFollowDistance = 36;
		private const int CloudSeatForwardOffset = 5;
		private static readonly int[] CloudFrames = { 0, 1, 2, 1 };
		private int f;
		private int cmtoY;
		private int cmdy;
		private int cmtoX;
		private int cmdx;
		private int fimg = -1;
		private int wimg;
		private int himg;
		private bool hasGroundPosition;
		private bool ridingCloud;
		private int groundMapId = -1;
		private bool IsGroundPet => GroundFootInset >= 0;

		private int[] frame = new int[4] { 0, 1, 2, 1 };

		private int count;

		public PetFollow()
		{
			f = Res.random(0, 3);
		}

		public void SetImg(int fimg, int[] frameNew, int wimg, int himg)
		{
			if (fimg >= 1)
			{
				this.fimg = fimg;
				frame = frameNew;
				this.wimg = wimg;
				this.himg = himg;
			}
		}

		public void paint(mGraphics g)
		{
			int w = 32;
			int h = 32;
			int num = ((GameCanvas.gameTick % 10 > 5) ? 1 : 0);
			if (fimg > 0)
			{
				w = wimg;
				h = himg;
				num = 0;
			}
			if (IsGroundPet && !hasGroundPosition && !ridingCloud) return;
			if (ridingCloud && Char.imgMount_TD != null)
			{
				int cloudFrame = CloudFrames[GameCanvas.gameTick / 2 % CloudFrames.Length];
				g.drawRegion(Char.imgMount_TD, 7, cloudFrame * 40 + 11, 32, 18,
					dir == 1 ? 2 : 0, cmx, cmy - 3, StaticObj.TOP_CENTER);
			}
			int drawX = ridingCloud ? cmx + (dir == 1 ? CloudSeatForwardOffset : -CloudSeatForwardOffset) : cmx;
			int drawY = IsGroundPet ? cmy + GroundFootInset : cmy + 3 + num;
			int anchor = IsGroundPet ? StaticObj.BOTTOM_HCENTER : StaticObj.VCENTER_HCENTER;
			SmallImage.drawSmallImage(g, smallID, f, drawX, drawY, w, h, dir != 1 ? 2 : 0, anchor);
		}

		public void FollowOwner(int ownerX, int ownerY, int ownerDirection, bool ownerFlying)
		{
			if (IsGroundPet)
			{
				FollowGroundPet(ownerX, ownerY, ownerDirection, ownerFlying);
				return;
			}

			if (GameCanvas.gameTick % 3 == 0)
			{
				if (ownerDirection == 1) cmtoX = ownerX - FlyingFollowDistance;
				if (ownerDirection == -1) cmtoX = ownerX + FlyingFollowDistance;
				cmtoY = ownerY - 40;
				dir = cmx > ownerX ? -1 : 1;
				cmtoX = System.Math.Min(System.Math.Max(cmtoX, 100), TileMap.pxw - 100);
			}
			moveCamera();
			AdvanceFrame();
		}

		private void FollowGroundPet(int ownerX, int ownerY, int ownerDirection, bool ownerFlying)
		{
			int tileSize = TileMap.size;
			if (tileSize <= 0 || TileMap.pxw <= 0 || TileMap.pxh <= 0) return;
			if (groundMapId != TileMap.mapID)
			{
				groundMapId = TileMap.mapID;
				hasGroundPosition = false;
				ridingCloud = false;
			}

			int targetX = System.Math.Max(0, System.Math.Min(ownerX - ownerDirection * GroundFollowDistance, TileMap.pxw - 1));
			int ownerFloor = TileMap.tileYofPixel(ownerY + 2);
			bool ownerOnFloor = System.Math.Abs(ownerY - ownerFloor) <= 2 && TileMap.IsMapEntryGround(ownerX, ownerFloor);
			if (ridingCloud || (ownerFlying && !ownerOnFloor))
			{
				if (ownerOnFloor)
				{
					int landingX = TileMap.IsMapEntryGround(targetX, ownerFloor) ? targetX : ownerX;
					FlyOnCloud(landingX, ownerFloor, ownerDirection);
					if (System.Math.Abs(cmx - landingX) <= 2 && System.Math.Abs(cmy - ownerFloor) <= 2)
						StandOnGround(landingX, ownerFloor, ownerDirection);
				}
				else
				{
					int cloudX = System.Math.Max(0, System.Math.Min(ownerX - ownerDirection * CloudFollowDistance, TileMap.pxw - 1));
					FlyOnCloud(cloudX, ownerY + 3, ownerDirection);
				}
				return;
			}
			if (!hasGroundPosition || (ownerOnFloor
					&& (System.Math.Abs(cmx - ownerX) > CatchUpDistance || System.Math.Abs(cmy - ownerFloor) > tileSize * 2)))
			{
				int floor = FindFloorBelow(targetX, ownerFloor, tileSize);
				if (floor < 0 || (ownerOnFloor && floor > ownerFloor + tileSize))
				{
					targetX = ownerX;
					floor = ownerOnFloor ? ownerFloor : FindFloorBelow(targetX, ownerFloor, tileSize);
				}
				if (floor >= 0)
				{
					StandOnGround(targetX, floor, ownerDirection);
					return;
				}
				if (!hasGroundPosition) return;
			}

			int distance = targetX - cmx;
			int nextX = cmx + System.Math.Sign(distance) * System.Math.Min(System.Math.Abs(distance), GroundSpeed);
			int nextY = cmy;
			if (!TileMap.IsMapEntryGround(nextX, nextY))
			{
				if (TileMap.IsMapEntryGround(nextX, cmy - tileSize)) nextY -= tileSize;
				else if (TileMap.IsMapEntryGround(nextX, cmy + tileSize)) nextY += tileSize;
				else if (ownerOnFloor && System.Math.Abs(cmx - ownerX) > GroundFollowDistance + tileSize)
				{
					int catchUpX = TileMap.IsMapEntryGround(targetX, ownerFloor) ? targetX : ownerX;
					StandOnGround(catchUpX, ownerFloor, ownerDirection);
					return;
				}
				else nextX = cmx;
			}
			if (nextX == cmx)
			{
				dir = ownerDirection;
				f = count = 0;
				return;
			}
			dir = nextX > cmx ? 1 : -1;
			cmx = nextX;
			cmy = nextY;
			AdvanceFrame();
		}

		private void FlyOnCloud(int x, int y, int direction)
		{
			if (!ridingCloud)
			{
				if (!hasGroundPosition)
				{
					cmx = x;
					cmy = y;
				}
				cmdx = cmdy = 0;
				ridingCloud = true;
				hasGroundPosition = false;
				f = count = 0;
			}
			cmtoX = x;
			cmtoY = y;
			dir = direction;
			moveCamera();
		}

		private void StandOnGround(int x, int y, int direction)
		{
			cmx = x;
			cmy = y;
			hasGroundPosition = true;
			ridingCloud = false;
			dir = direction;
			f = count = 0;
		}

		private static int FindFloorBelow(int x, int startY, int tileSize)
		{
			for (int y = System.Math.Max(0, startY); y < TileMap.pxh; y += tileSize)
				if (TileMap.IsMapEntryGround(x, y)) return y;
			return -1;
		}

		private void AdvanceFrame()
		{
			if (GameCanvas.gameTick % 3 == 0)
			{
				f = frame[count];
				count++;
			}
			if (count >= frame.Length)
			{
				count = 0;
			}
		}

		public void remove()
		{
			if (IsGroundPet && !hasGroundPosition && !ridingCloud) return;
			int y = IsGroundPet ? cmy - 6 : cmy + 3 + (GameCanvas.gameTick % 10 > 5 ? 1 : 0);
			ServerEffect.addServerEffect(60, cmx, y, 1);
		}

		private void moveCamera()
		{
			if (cmy != cmtoY)
			{
				cmdy += (cmtoY - cmy) << 2;
				cmy += cmdy >> 4;
				cmdy &= 15;
			}
			if (cmx != cmtoX)
			{
				cmdx += (cmtoX - cmx) << 2;
				cmx += cmdx >> 4;
				cmdx &= 15;
			}
		}
	}
}
