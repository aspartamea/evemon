﻿using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using EVEMon.Common;
using EVEMon.Common.CustomEventArgs;
using EVEMon.Common.ExternalCalendar;
using EVEMon.Common.Scheduling;

namespace EVEMon.CharacterMonitoring
{
    public sealed partial class CharacterMonitorFooter : UserControl
    {
        private Character m_character;


        #region Constructor

        public CharacterMonitorFooter()
        {
            InitializeComponent();
        }

        #endregion


        #region Inherited Events

        /// <summary>
        /// Occurs when control loads.
        /// </summary>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (DesignMode || this.IsDesignModeHosted())
                return;

            // Fonts
            Font = FontFactory.GetFont("Tahoma");
            lblScheduleWarning.Font = FontFactory.GetFont("Tahoma", FontStyle.Bold);

            CCPCharacter ccpCharacter = m_character as CCPCharacter;

            if (ccpCharacter != null)
                skillQueueControl.SkillQueue = ccpCharacter.SkillQueue;
            else
            {
                pnlTraining.Visible = false;
                skillQueuePanel.Visible = false;
            }

            // Subscribe events
            EveMonClient.TimerTick += EveMonClient_TimerTick;
            EveMonClient.SettingsChanged += EveMonClient_SettingsChanged;
            EveMonClient.SchedulerChanged += EveMonClient_SchedulerChanged;
            EveMonClient.CharacterSkillQueueUpdated += EveMonClient_CharacterSkillQueueUpdated;
            Disposed += OnDisposed;

            base.OnLoad(e);
        }

        /// <summary>
        /// Occurs when visibility changes.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);

            if (!Visible)
                return;

            UpdateFrequentControls();
            UpdateInfrequentControls();
        }

        /// <summary>
        /// Called when the control is disposed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void OnDisposed(object sender, EventArgs e)
        {
            EveMonClient.TimerTick -= EveMonClient_TimerTick;
            EveMonClient.SettingsChanged -= EveMonClient_SettingsChanged;
            EveMonClient.SchedulerChanged -= EveMonClient_SchedulerChanged;
            EveMonClient.CharacterSkillQueueUpdated -= EveMonClient_CharacterSkillQueueUpdated;
            Disposed -= OnDisposed;
        }

        #endregion


        #region Update display on character change

        /// <summary>
        /// Updates the controls whos content changes frequently.
        /// </summary>
        private void UpdateFrequentControls()
        {
            SuspendLayout();
            try
            {
                // Update the training controls
                UpdateTrainingControls();
            }
            finally
            {
                ResumeLayout();
            }
        }

        /// <summary>
        /// Updates the informations for skill training.
        /// </summary>
        private void UpdateTrainingControls()
        {
            // No need to do anything when the control is not visible
            if (!Visible)
                return;

            // Is the character in training ?
            if (m_character.IsTraining)
            {
                UpdateTrainingSkillInfo();

                UpdateSkillQueueInfo();

                skillQueuePanel.Visible = true;
                pnlTraining.Visible = true;
                lblPaused.Visible = false;
                return;
            }

            // Not in training, check for paused skill queue
            if (SkillQueueIsPaused())
                return;

            // Not training, no skill queue
            skillQueuePanel.Visible = false;
            pnlTraining.Visible = false;
            lblPaused.Visible = false;
        }

        /// <summary>
        /// Updates the training skill info.
        /// </summary>
        private void UpdateTrainingSkillInfo()
        {
            QueuedSkill training = m_character.CurrentlyTrainingSkill;
            DateTime completionTime = training.EndTime.ToLocalTime();

            lblTrainingSkill.Text = training.ToString();
            lblSPPerHour.Text = (training.Skill == null
                                     ? "???"
                                     : String.Format(CultureConstants.DefaultCulture, "{0} SP/Hour",
                                                     training.Skill.SkillPointsPerHour));
            lblTrainingEst.Text = String.Format(CultureConstants.DefaultCulture, "{0:ddd} {1:G}", completionTime, completionTime);

            // Dipslay a warning if anything scheduled is blocking us
            string conflictMessage;
            lblScheduleWarning.Visible = Scheduler.SkillIsBlockedAt(training.EndTime.ToLocalTime(), out conflictMessage);
            lblScheduleWarning.Text = conflictMessage;
        }

        /// <summary>
        /// Updates the skill queue info.
        /// </summary>
        private void UpdateSkillQueueInfo()
        {
            CCPCharacter ccpCharacter = m_character as CCPCharacter;
            if (ccpCharacter == null)
                return;

            DateTime queueCompletionTime = ccpCharacter.SkillQueue.EndTime.ToLocalTime();
            lblQueueCompletionTime.Text = String.Format(CultureConstants.DefaultCulture,
                                                        "{0:ddd} {0:G}", queueCompletionTime);

            // Skill queue time panel
            skillQueueTimePanel.Visible = ccpCharacter.SkillQueue.Count > 1 || Settings.UI.MainWindow.AlwaysShowSkillQueueTime ||
                                          (ccpCharacter.SkillQueue.Count == 1 && Settings.UI.MainWindow.AlwaysShowSkillQueueTime);

            // Update the remaining training time label
            QueuedSkill training = m_character.CurrentlyTrainingSkill;
            lblTrainingRemain.Text = training.EndTime.ToRemainingTimeDescription(DateTimeKind.Utc);

            // Update the remaining queue time label
            DateTime queueEndTime = ccpCharacter.SkillQueue.EndTime;
            lblQueueRemaining.Text = queueEndTime.ToRemainingTimeDescription(DateTimeKind.Utc);
        }

        /// <summary>
        /// Updates the skill queue info if queue is paused.
        /// </summary>
        /// <returns></returns>
        private bool SkillQueueIsPaused()
        {
            CCPCharacter ccpCharacter = m_character as CCPCharacter;
            if (ccpCharacter == null || !ccpCharacter.SkillQueue.IsPaused)
                return false;

            QueuedSkill training = ccpCharacter.SkillQueue.CurrentlyTraining;
            lblTrainingSkill.Text = training.ToString();
            lblSPPerHour.Text = (training.Skill == null
                                     ? "???"
                                     : String.Format(CultureConstants.DefaultCulture, "{0} SP/Hour",
                                                     training.Skill.SkillPointsPerHour));

            lblTrainingRemain.Text = "Paused";
            lblTrainingEst.Text = String.Empty;
            lblScheduleWarning.Visible = false;
            skillQueueTimePanel.Visible = false;
            skillQueuePanel.Visible = true;
            pnlTraining.Visible = true;
            lblPaused.Visible = true;

            return true;
        }

        /// <summary>
        /// Updates the controls whos content changes infrequently.
        /// </summary>
        private void UpdateInfrequentControls()
        {
            // No need to do anything when the control is not visible
            if (!Visible)
                return;

            SuspendLayout();
            try
            {
                // "Update Calendar" button
                btnAddToCalendar.Visible = Settings.Calendar.Enabled;
            }
            finally
            {
                ResumeLayout();
            }
        }

        #endregion


        #region Global Events

        /// <summary>
        /// Occur on every second.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void EveMonClient_TimerTick(object sender, EventArgs e)
        {
            UpdateFrequentControls();
        }

        /// <summary>
        /// Updates the controls on settings change.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void EveMonClient_SettingsChanged(object sender, EventArgs e)
        {
            UpdateInfrequentControls();
        }

        /// <summary>
        /// When the scheduler changed, we need to check the conflicts.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void EveMonClient_SchedulerChanged(object sender, EventArgs e)
        {
            UpdateTrainingControls();
        }

        /// <summary>
        /// Occur when the character skill queue updates.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CharacterChangedEventArgs"/> instance containing the event data.</param>
        private void EveMonClient_CharacterSkillQueueUpdated(object sender, CharacterChangedEventArgs e)
        {
            if (e.Character != m_character)
                return;

            skillQueueControl.Invalidate();
        }

        #endregion


        #region Local Events

        /// <summary>
        /// Occurs when the user click the "Update Calendar" button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void btnUpdateCalendar_Click(object sender, EventArgs e)
        {
            // Ensure that we are trying to use the external calendar
            if (!Settings.Calendar.Enabled)
            {
                btnAddToCalendar.Visible = false;
                return;
            }

            if (m_character is CCPCharacter)
                ExternalCalendar.UpdateCalendar(m_character as CCPCharacter);
        }

        #endregion


        #region Helper Methods

        /// <summary>
        /// Sets the character.
        /// </summary>
        /// <value>The character.</value>
        public void SetCharacter(Character character)
        {
            if (m_character == character)
                return;

            m_character = character;
        }

        #endregion
    }
}