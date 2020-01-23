using System;
using System.Collections.Generic;
using System.Linq;
using LiteDB;
using Newtonsoft.Json;
using restapi.Helpers;

namespace restapi.Models
{
    public class Timecard
    {
        public Timecard() { }

        public Timecard(int person)
        {
            Opened = DateTime.UtcNow;
            Employee = person;
            UniqueIdentifier = Guid.NewGuid();
            Lines = new List<TimecardLine>();
            Transitions = new List<Transition>();
        }

        public int Employee { get; set; }

        public TimecardStatus Status
        {
            get
            {
                return Transitions
                    .OrderByDescending(t => t.OccurredAt)
                    .First()
                    .TransitionedTo;
            }
        }

        [BsonIgnore]
        [JsonProperty("_self")]
        public string Self { get => $"/timesheets/{UniqueIdentifier}"; }

        public DateTime Opened { get; set; }

        [JsonIgnore]
        [BsonId]
        public ObjectId Id { get; set; }

        [JsonProperty("id")]
        public Guid UniqueIdentifier { get; set; }

        [JsonIgnore]
        public IList<TimecardLine> Lines { get; set; }

        [JsonIgnore]
        public IList<Transition> Transitions { get; set; }

        public IList<ActionLink> Actions { get => GetActionLinks(); }

        [JsonProperty("documentation")]
        public IList<DocumentLink> Documents { get => GetDocumentLinks(); }

        public string Version { get; set; } = "timecard-0.1";

        private IList<ActionLink> GetActionLinks()
        {
            var links = new List<ActionLink>();

            switch (Status)
            {
                case TimecardStatus.Draft:
                    links.Add(new ActionLink()
                    {
                        Method = Method.Post,
                        Type = ContentTypes.Cancellation,
                        Relationship = ActionRelationship.Cancel,
                        Reference = $"/timesheets/{UniqueIdentifier}/cancellation"
                    });

                    links.Add(new ActionLink()
                    {
                        Method = Method.Post,
                        Type = ContentTypes.Submittal,
                        Relationship = ActionRelationship.Submit,
                        Reference = $"/timesheets/{UniqueIdentifier}/submittal"
                    });

                    links.Add(new ActionLink()
                    {
                        Method = Method.Post,
                        Type = ContentTypes.TimesheetLine,
                        Relationship = ActionRelationship.RecordLine,
                        Reference = $"/timesheets/{UniqueIdentifier}/lines"
                    });

                    break;

                case TimecardStatus.Submitted:
                    links.Add(new ActionLink()
                    {
                        Method = Method.Post,
                        Type = ContentTypes.Cancellation,
                        Relationship = ActionRelationship.Cancel,
                        Reference = $"/timesheets/{UniqueIdentifier}/cancellation"
                    });

                    links.Add(new ActionLink()
                    {
                        Method = Method.Post,
                        Type = ContentTypes.Rejection,
                        Relationship = ActionRelationship.Reject,
                        Reference = $"/timesheets/{UniqueIdentifier}/rejection"
                    });

                    links.Add(new ActionLink()
                    {
                        Method = Method.Post,
                        Type = ContentTypes.Approval,
                        Relationship = ActionRelationship.Approve,
                        Reference = $"/timesheets/{UniqueIdentifier}/approval"
                    });

                    break;

                case TimecardStatus.Approved:
                    // terminal state, nothing possible here
                    break;

                case TimecardStatus.Cancelled:
                    // terminal state, nothing possible here
                    break;
            }

            return links;
        }

        private IList<DocumentLink> GetDocumentLinks()
        {
            var links = new List<DocumentLink>();

            links.Add(new DocumentLink()
            {
                Method = Method.Get,
                Type = ContentTypes.Transitions,
                Relationship = DocumentRelationship.Transitions,
                Reference = $"/timesheets/{UniqueIdentifier}/transitions"
            });

            if (this.Lines.Count > 0)
            {
                links.Add(new DocumentLink()
                {
                    Method = Method.Get,
                    Type = ContentTypes.TimesheetLine,
                    Relationship = DocumentRelationship.Lines,
                    Reference = $"/timesheets/{UniqueIdentifier}/lines"
                });
            }

            if (this.Status == TimecardStatus.Submitted)
            {
                links.Add(new DocumentLink()
                {
                    Method = Method.Get,
                    Type = ContentTypes.Transitions,
                    Relationship = DocumentRelationship.Submittal,
                    Reference = $"/timesheets/{UniqueIdentifier}/submittal"
                });
            }

            return links;
        }

        public TimecardLine AddLine(DocumentLine documentLine)
        {
            var annotatedLine = new TimecardLine(documentLine);

            Lines.Add(annotatedLine);

            return annotatedLine;
        }

        //[MN] ADDED LOGIC FOR REPLACE LINE
        public TimecardLine ReplaceLine(string oldDocId, DocumentLine newDocLine)
        {
            TimecardLine result = null;

            // SEARCH for line with oldDocID and REPLACE it
            for (int i = 0; i < Lines.Count; i++)
            {
                if (Lines[i].UniqueIdentifier.ToString() == oldDocId)
                {
                    result = Lines[i];
                    Lines[i].Week = newDocLine.Week;
                    Lines[i].Year = newDocLine.Year;
                    Lines[i].Day = newDocLine.Day;
                    Lines[i].Hours = newDocLine.Hours;
                    Lines[i].Project = newDocLine.Project;
                    break;
                }
            }

            return result;
        }

        //[MN] ADDED LOGIC FOR PATCH LINE
        public TimecardLine PatchLine(string oldDocId, DocumentLinePatch newDocPatch)
        {
            TimecardLine result = null;

            // SEARCH for line with oldDocID and REPLACE it
            for (int i = 0; i < Lines.Count; i++)
            {
                if (Lines[i].UniqueIdentifier.ToString() == oldDocId)
                {
                    result = Lines[i];
                    if (newDocPatch.Week.HasValue) Lines[i].Week = newDocPatch.Week.Value;
                    if (newDocPatch.Year.HasValue) Lines[i].Year = newDocPatch.Year.Value;
                    if (newDocPatch.Day.HasValue) Lines[i].Day = newDocPatch.Day.Value;
                    if (newDocPatch.Hours.HasValue) Lines[i].Hours = newDocPatch.Hours.Value;
                    if (newDocPatch.Project.Length > 0) Lines[i].Project = newDocPatch.Project;
                    break;
                }
            }

            return result;
        }

        public bool CanBeDeleted()
        {
            return (Status == TimecardStatus.Cancelled || Status == TimecardStatus.Draft);
        }

        public bool HasLine(Guid lineId)
        {
            return Lines
                .Any(l => l.UniqueIdentifier == lineId);
        }


        public override string ToString()
        {
            return PublicJsonSerializer.SerializeObjectIndented(this);
        }
    }
}