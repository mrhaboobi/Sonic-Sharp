﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace SonicSharp
{
    /// <summary>
    /// The base Player class. Based on the wonderful
    /// Sonic Physics Guide from the Sonic Retro Wiki!
    /// </summary>
    public class Player : GameObject
    {
        // Variables/Constants
        public Sprite IdleSprite, WalkingSprite, RunningSprite,
            BrakingSprite, DeadSprite, JumpingSprite, TurnAroundSprite;

        public Color Color = Color.White;
        public float Speed, XSpeed, YSpeed, Angle;
        public float Acceleration = 0.046875f, Deceleration = 0.5f, Friction = 0.046875f,
            TopSpeed = 6, BrakeThreshold = 4.5f, Gravity = 0.21875f, TopYSpeed = 16;

        public Input.Devices InputDevice = Input.Devices.Keyboard;
        public SpriteEffects Effects = SpriteEffects.None;
        public bool IsFalling = true;

        public const int Width = 40, Height = 40,
            SpinWidth = 30, SpinHeight = 30;

        public const int OriginX = 20, OriginY = 20,
            SpinOriginX = 15, SpinOriginY = 15;

        // Methods
        public override void Init()
        {
            Sprite = IdleSprite;
            IsFalling = false; // TODO: PLEASE REMOVE THIS LINE ONCE DONE DEBUGGING THX
        }

        public override void Update()
        {
            // Ground Movement
            if (GameWindow.Inputs["Left"].IsDown(InputDevice) ||
                GameWindow.Inputs["AltLeft"].IsDown(InputDevice))
            {
                if (Speed > 0)
                {
                    if (Speed >= BrakeThreshold)
                        Sprite = BrakingSprite;

                    Speed -= Deceleration;
                }
                else if (Speed > -TopSpeed)
                {
                    if (Sprite == BrakingSprite)
                        Sprite = TurnAroundSprite;

                    Effects = SpriteEffects.FlipHorizontally;
                    Speed -= Acceleration;
                }
            }
            else if (GameWindow.Inputs["Right"].IsDown(InputDevice) ||
                GameWindow.Inputs["AltRight"].IsDown(InputDevice))
            {
                if (Speed < 0)
                {
                    if (Math.Abs(Speed) >= BrakeThreshold)
                        Sprite = BrakingSprite;

                    Speed += Deceleration;
                }
                else if (Speed < TopSpeed)
                {
                    if (Sprite == BrakingSprite)
                        Sprite = TurnAroundSprite;

                    Effects = SpriteEffects.None;
                    Speed += Acceleration;
                }
            }
            else
            {
                Speed -= Math.Min(Math.Abs(Speed),
                    Friction) * Math.Sign(Speed);
            }

            // TODO

            // Update Position
            if (!IsFalling)
            {
                XSpeed = Speed * (float)Math.Cos(Angle);
                YSpeed = Speed * -(float)Math.Sin(Angle);
            }
            else
            {
                // Gravity
                YSpeed += Gravity;
                if (YSpeed > TopYSpeed)
                    YSpeed = TopYSpeed;
            }

            Position.X += XSpeed;
            Position.Y += YSpeed;

            // Horizontal Collision
            Block block;
            int posX, biX, blockX, tiX, tileIndex;
            int posY, biY, blockY, tiY, yoff;
            posY = ((int)Position.Y); // TODO: Make this Position.Y + 8 if on flat ground?

            if (posY >= 0 && posY <= GameWindow.CurrentStage.RowCount * Block.BlockSize)
            {
                biY = (posY / (int)Block.BlockSize);
                blockY = (biY * (int)Block.BlockSize);
                tiY = ((posY - blockY) / (int)Tile.TileSize);
                yoff = (tiY * (int)Block.TilesPerRow);

                for (int i = -10; i <= 10; ++i)
                {
                    posX = ((int)Position.X + i);
                    if (posX <= -Tile.TileSize || posX >
                        GameWindow.CurrentStage.ColumnCount * Block.BlockSize)
                    {
                        continue;
                    }

                    biX = (posX / (int)Block.BlockSize);
                    blockX = (biX * (int)Block.BlockSize);
                    tiX = ((posX - blockX) / (int)Tile.TileSize);
                    block = GameWindow.CurrentStage.GetBlock((uint)biY, (uint)biX);

                    if (block == null)
                        continue;

                    if (block.Tiles[tiX + yoff] != 0)
                    {
                        Speed = 0;

                        int tileXPos = (tiX * (int)Tile.TileSize) + blockX;
                        Position.X = (tileXPos > Position.X) ?
                            tileXPos - 10 : tileXPos + (int)Tile.TileSize + 10;
                        break;
                    }
                }
            }
            else
            {
                biY = yoff = 0;
            }

            // Vertical Collision
            var sensorA = VerticalCollisionCheck(-9, out byte angleA);
            var sensorB = VerticalCollisionCheck(9, out byte angleB);

            if (!(IsFalling = (!sensorA.HasValue && !sensorB.HasValue)))
            {
                YSpeed = 0;
                if (!sensorB.HasValue || (sensorA.HasValue &&
                    sensorA.Value < sensorB.Value))
                {
                    Position.Y = sensorA.Value;
                    Angle = MathHelper.ToRadians((256 - angleA) * 1.40625f);
                }
                else
                {
                    Position.Y = sensorB.Value;
                    Angle = MathHelper.ToRadians((256 - ((sensorA.HasValue &&
                        sensorB.HasValue && sensorA.Value == sensorB.Value &&
                        angleA < angleB) ? angleA : angleB)) * 1.40625f);
                }
            }
            else
            {
                // Backup Sensor for when you're exactly in the center of a tile
                var middleSensor = VerticalCollisionCheck(0, out byte angleMiddle);
                if (!(IsFalling = !middleSensor.HasValue))
                {
                    YSpeed = 0;
                    Position.Y = middleSensor.Value;
                    Angle = MathHelper.ToRadians((256 - angleMiddle) * 1.40625f);
                }
                else
                {
                    Angle = 0;
                }
            }

            // Update Sprite
            if (!IsFalling && ((Sprite != BrakingSprite &&
                Sprite != TurnAroundSprite) || Sprite.HasLooped))
            {
                Sprite = (Speed == 0) ? IdleSprite :
                    (Math.Abs(Speed) >= TopSpeed) ?
                    RunningSprite : WalkingSprite;
            }

            // Animate Sprite
            if (Sprite == null) return;
            if (IsFalling || Speed != 0)
            {
                Sprite.Animate(Math.Max(((IsFalling) ? 5 : 8) -
                    Math.Abs(Speed), 1));
            }
            else
            {
                Sprite.Animate();
            }
            
            // Sub-Methods
            float? VerticalCollisionCheck(int xoffset, out byte angle)
            {
                posX = ((int)Position.X + xoffset);
                if (posX < 0 || posX > GameWindow.CurrentStage.ColumnCount * Block.BlockSize)
                {
                    angle = 0;
                    return null;
                }

                biX = (posX / (int)Block.BlockSize);
                blockX = (biX * (int)Block.BlockSize);
                tiX = ((posX - blockX) / (int)Tile.TileSize);

                for (int i = 0; i <= 20; ++i)
                {
                    posY = ((int)Position.Y + i);
                    if (posY <= -Tile.TileSize || posY >
                        GameWindow.CurrentStage.RowCount * Block.BlockSize)
                    {
                        continue;
                    }

                    biY = (posY / (int)Block.BlockSize);
                    blockY = (biY * (int)Block.BlockSize);
                    tiY = ((posY - blockY) / (int)Tile.TileSize);
                    block = GameWindow.CurrentStage.GetBlock((uint)biY, (uint)biX);

                    if (block == null)
                        continue;

                    tileIndex = tiX + (tiY * (int)Block.TilesPerRow);
                    if (block.Tiles[tileIndex] != 0)
                    {
                        // It's more performant to copy the struct than index the array twice
                        var tile = GameWindow.CurrentStage.Tiles[block.Tiles[tileIndex]];
                        int tileXPos = (tiX * (int)Tile.TileSize) + blockX;
                        int tileYPos = (tiY * (int)Tile.TileSize) + blockY;

                        angle = tile.Angle;
                        var h = tile.GetHeight((byte)(posX - tileXPos));
                        return (tileYPos + (16 - h)) - 20;
                    }
                }

                angle = 0;
                return null;
            }
        }

        public override void Draw()
        {
            if (Sprite == null) return;
            Sprite.Draw(Position, Effects, Color, Angle);
        }
    }
}