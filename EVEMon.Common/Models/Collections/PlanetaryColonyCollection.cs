﻿using System;
using System.Collections.Generic;
using System.Linq;
using EVEMon.Common.Collections;
using EVEMon.Common.Enumerations;
using EVEMon.Common.Enumerations.API;
using EVEMon.Common.Interfaces;
using EVEMon.Common.Serialization.API;

namespace EVEMon.Common.Models.Collections
{
    public sealed class PlanetaryColonyCollection : ReadonlyCollection<PlanetaryColony>
    {
        private readonly CCPCharacter m_ccpCharacter;


        #region Constructor

        /// <summary>
        /// Internal constructor.
        /// </summary>
        /// <param name="ccpCharacter">The CCP character.</param>
        internal PlanetaryColonyCollection(CCPCharacter ccpCharacter)
        {
            m_ccpCharacter = ccpCharacter;

            EveMonClient.TimerTick += EveMonClient_TimerTick;
        }

        #endregion


        #region Inherited Events

        /// <summary>
        /// Called when the object gets disposed.
        /// </summary>
        internal void Dispose()
        {
            EveMonClient.TimerTick -= EveMonClient_TimerTick;
        }

        /// <summary>
        /// Handles the TimerTick event of the EveMonClient control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void EveMonClient_TimerTick(object sender, EventArgs e)
        {
            IQueryMonitor charPlanetaryColoniesMonitor = m_ccpCharacter.QueryMonitors[APIGenericMethods.PlanetaryColonies];

            if (charPlanetaryColoniesMonitor == null || !charPlanetaryColoniesMonitor.Enabled)
                return;

            UpdateOnTimerTick();
        }

        #endregion


        #region Importation

        /// <summary>
        /// Imports an enumeration of API objects.
        /// </summary>
        /// <param name="src">The enumeration of serializable planetary colony log from the API.</param>
        internal void Import(IEnumerable<SerializablePlanetaryColony> src)
        {
            Items.Clear();

            // Import the palnetary colony from the API
            foreach (SerializablePlanetaryColony srcColony in src)
            {
                Items.Add(new PlanetaryColony(m_ccpCharacter, srcColony));
            }
        }

        #endregion


        #region Helper Methods

        /// <summary>
        /// Notify the user on a pin completion.
        /// </summary>
        private void UpdateOnTimerTick()
        {
            // We exit if there are no pins
            if (!Items.Any())
                return;

            // Add the not notified idle pins to the completed list
            List<PlanetaryPin> pinsCompleted = Items.SelectMany(x => x.Pins).Where(
                pin => pin.State == PlanetaryPinState.Idle && pin.TTC.Length == 0 && !pin.NotificationSend).ToList();

            pinsCompleted.ForEach(pin => pin.NotificationSend = true);

            // We exit if no pins have finished
            if (!pinsCompleted.Any())
                return;

            // Fires the event regarding the character's pins finished
            EveMonClient.OnCharacterPlanetaryPinsCompleted(m_ccpCharacter, pinsCompleted);
        }

        #endregion
    }
}