using Game.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.InputSystem.LowLevel;

namespace ZoningToolkit.Utils
{
    internal class GameUtils
    {
        /// <summary>
        /// Copies a game action binding to a mod-usable <see cref="ProxyBinding" />.
        /// </summary>
        /// <param name="gameActionName">Game action name to copy from.</param>
        /// <param name="modActionName">Mod action name to copy to.</param>
        /// <returns>New <see cref="ProxyBinding" /> bound to the default game action.</returns>
        public static ProxyAction CopyGameAction(string gameActionName, string modActionName, string nameOfClass)
        {
            LogUtils.getLogger().Debug($"Copying Game Action {gameActionName} to Mod Action {modActionName}");
            // Get action references.
            ProxyAction modAction = Mod.Instance.ActiveSettings.GetAction(modActionName);
            ProxyAction gameAction = InputManager.instance.FindAction(InputManager.kToolMap, gameActionName);

            if (modAction == null)
            {
                LogUtils.getLogger().Debug("Mod binding is null. Therefore, no watcher on binding will be set.");
                return null;
            }

            // Enable mod action.
            modAction.shouldBeEnabled = true;

            // Find action bindings.
            ProxyBinding modBinding = modAction.bindings.FirstOrDefault(b => b.group == nameOfClass && b.actionName == modActionName);
            ProxyBinding gameBinding = gameAction.bindings.FirstOrDefault(b => b.group == nameOfClass && b.actionName == gameActionName);

            if (gameBinding == default)
            {
                LogUtils.getLogger().Debug("Game Binding is default. Therefore, no watcher on binding will be set.");
                return null;
            }

            if (modBinding == default)
            {
                LogUtils.getLogger().Debug("Mod Binding is default. Therefore, no watcher on binding will be set.");
                return null;
            }


            // Setup change watcher and apply current settings.
            ProxyBinding.Watcher applyWatcher = new ProxyBinding.Watcher(gameBinding, binding => BindToGameAction(modBinding, binding));
            BindToGameAction(modBinding, applyWatcher.binding);

            return modAction;
        }

        /// <summary>
        /// Binds a ProxyBinding to a game action.
        /// </summary>
        /// <param name="mimicBinding">ProxyBinding to bind.</param>
        /// <param name="gameAction">Game action to bind to.</param>
        private static void BindToGameAction(ProxyBinding mimicBinding, ProxyBinding gameAction)
        {
            ProxyBinding newBinding = mimicBinding.Copy();
            newBinding.path = gameAction.path;
            newBinding.modifiers = gameAction.modifiers;
            InputManager.instance.SetBinding(newBinding, out _);
        }
    }
}
