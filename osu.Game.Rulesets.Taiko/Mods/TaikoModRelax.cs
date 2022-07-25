// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.Taiko.Objects;
using osu.Game.Rulesets.Taiko.Objects.Drawables;
using osu.Game.Rulesets.UI;
using static osu.Game.Input.Handlers.ReplayInputHandler;

namespace osu.Game.Rulesets.Taiko.Mods
{
    public class TaikoModRelax : ModRelax, IApplicableToDrawableRuleset<TaikoHitObject>, IApplicableToDrawableHitObject
    {
        public override string Description => @"No ninja-like spinners, demanding drumrolls or unexpected katu's.";

        protected DrawableRuleset<TaikoHitObject> drawableRuleset;
        protected TaikoInputManager taikoInputManager;

        public void ApplyToDrawableRuleset(DrawableRuleset<TaikoHitObject> drawableRuleset)
        {
            this.drawableRuleset = drawableRuleset;
            this.taikoInputManager = (TaikoInputManager)drawableRuleset.KeyBindingInputManager;

            drawableRuleset.KeyBindingInputManager.Add(new InputCorrector(this));
        }

        public void ApplyToDrawableHitObject(DrawableHitObject drawable)
        {
            if (drawable is DrawableSwell swell)
            {
                swell.RequireAlternatingHits = false;
            }
        }

        private TaikoAction correctInput(TaikoAction action)
        {
            DrawableHit firstHit = findFirstActiveDrawableHit();
            if (firstHit == null || firstHit.HitActions.Contains(action))
            {
                // No correction is necessary.
                return action;
            }

            // Only correct to actions on the same side of the drum as the incorrect action.
            return firstHit.HitActions.FirstOrDefault(a => getDrumSide(a) == getDrumSide(action));
        }

        private bool getDrumSide(TaikoAction action)
        {
            switch (action)
            {
                case TaikoAction.LeftRim:
                case TaikoAction.LeftCentre:
                    return false;
                case TaikoAction.RightCentre:
                case TaikoAction.RightRim:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action));
            }
        }

        private DrawableHit findFirstActiveDrawableHit()
        {
            double time = drawableRuleset.Playfield.Clock.CurrentTime;

            foreach (var h in drawableRuleset.Playfield.HitObjectContainer.AliveObjects.OfType<DrawableHit>())
            {
                double start = h.HitObject.StartTime;
                double missWindowWidth = h.HitObject.HitWindows.WindowFor(HitResult.Miss);

                // There is no DrawableHit with an active window yet.
                if (time < start - missWindowWidth)
                {
                    break;
                }

                // The earliest DrawableHit was already hit, or its window has passed. Try the next one.
                if (h.IsHit || time > start + missWindowWidth)
                {
                    continue;
                }

                return h;
            }

            return null;
        }

        private class InputCorrector : Component, IKeyBindingHandler<TaikoAction>
        {
            private readonly TaikoModRelax mod;

            public InputCorrector(TaikoModRelax mod)
            {
                this.mod = mod;
            }

            public bool OnPressed(KeyBindingPressEvent<TaikoAction> e)
            {
                TaikoAction? correctAction = mod.correctInput(e.Action);
                if (correctAction == null || correctAction == e.Action)
                    return false;

                //Key down
                new ReplayState<TaikoAction>
                {
                    PressedActions = new List<TaikoAction>() {
                        correctAction.Value
                    }
                }.Apply(mod.taikoInputManager.CurrentState, mod.taikoInputManager);

                //Key up
                new ReplayState<TaikoAction>
                {
                    PressedActions = new List<TaikoAction>()
                }.Apply(mod.taikoInputManager.CurrentState, mod.taikoInputManager);

                return true;
            }

            public void OnReleased(KeyBindingReleaseEvent<TaikoAction> e)
            {

            }
        }
    }
}
