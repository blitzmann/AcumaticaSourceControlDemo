﻿using PX.Data;
using PX.Objects.CS;
using PX.Objects.GL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.PM
{
	[PXDBInt()]
	[PXUIField(DisplayName = "Cost Code", FieldClass = COSTCODE)]
	public class CostCodeAttribute : AcctSubAttribute, IPXRowPersistingSubscriber, IPXFieldVerifyingSubscriber, IPXRowSelectedSubscriber
	{
		public const string COSTCODE = "COSTCODE";
		protected Type task;
		public bool AllowNullValue { get; set; }
		public bool SkipVerification { get; set; }
		public bool SkipVerificationForDefault { get; set; }
		public Type ReleasedField { get; set; }

		public CostCodeAttribute() : this(null, null, null)
		{
		}

		public CostCodeAttribute(Type account, Type task) : this(account, task, null)
		{

		}

		public CostCodeAttribute(Type account, Type task, string budgetType) : this(account, task, budgetType, null) { }

		public CostCodeAttribute(Type account, Type task, string budgetType, Type accountGroup): this(account, task, budgetType, accountGroup, false) { }

		public CostCodeAttribute(Type account, Type task, string budgetType, Type accountGroup, bool disableProjectSpecific)
		{
			this.task = task;
			CostCodeDimensionSelectorAttribute select = new CostCodeDimensionSelectorAttribute(account, task, budgetType, accountGroup, disableProjectSpecific);

			_Attributes.Add(select);
			_SelAttrIndex = _Attributes.Count - 1;
		}

		public static bool UseCostCode()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.costCodes>();
		}

		public static int GetDefaultCostCode()
		{
			return DefaultCostCode.GetValueOrDefault();
		}

		public static int? DefaultCostCode
		{
			get
			{
				CostCodeManager ccm = new CostCodeManager();
				return ccm.DefaultCostCodeID;
			}
		}


		public void RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			if (task == null)
				return;

			if (AllowNullValue)
				return;

			if (!UseCostCode())
				return;

			int? taskID = (int?)sender.GetValue(e.Row, task.Name);
			if (taskID == null)
				return;

			int? costCodeID = (int?)sender.GetValue(e.Row, FieldOrdinal);

			if (costCodeID == null)
			{
				if (sender.RaiseExceptionHandling(FieldName, e.Row, null, new PXSetPropertyException(Data.ErrorMessages.FieldIsEmpty, FieldName)))
				{
					throw new PXRowPersistingException(FieldName, null, Data.ErrorMessages.FieldIsEmpty, FieldName);
				}

			}
		}

		public void FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			if (task != null && !SkipVerification && e.NewValue != null && UseCostCode())
			{
				if (SkipVerificationForDefault == true && (int)e.NewValue == GetDefaultCostCode())
					return;

				if (!IsValid((int)e.NewValue))
				{
					sender.RaiseExceptionHandling(FieldName, e.Row, e.NewValue, new PXSetPropertyException(Messages.CostCodeNotInBudget, PXErrorLevel.Warning));
				}
			}
		}

		protected virtual bool IsValid(int costCode)
		{
			return ((CostCodeDimensionSelectorAttribute)_Attributes[_SelAttrIndex]).IsProjectSpecific(costCode);
		}

		public void RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			if (SkipVerification)
				return;

			if (e.Row == null)
				return;

			if (!UseCostCode())
				return;

			int? costCode = (int?) sender.GetValue(e.Row, _FieldName);
			if (costCode == null)
				return;

			if (ReleasedField != null)
			{
				bool? released = (bool?)sender.GetValue(e.Row, ReleasedField.Name);
				if (released == true)
				{
					return;
				}
			}

			if (SkipVerificationForDefault == true && costCode == GetDefaultCostCode())
				return;

			if (!IsValid(costCode.Value))
			{
				PXUIFieldAttribute.SetWarning(sender, e.Row, _FieldName, Messages.CostCodeNotInBudget);
			}
			else
			{
				PXUIFieldAttribute.SetError(sender, e.Row, _FieldName, null);
			}
		}
	}


	public class CostCodeDimensionSelectorAttribute : PXDimensionSelectorAttribute
	{
		public CostCodeDimensionSelectorAttribute(Type account, Type task, string budgetType, Type accountGroup, bool disableProjectSpecific) : base(CostCodeAttribute.COSTCODE, typeof(Search<PMCostCode.costCodeID>), typeof(PMCostCode.costCodeCD))
		{
			this.DescriptionField = typeof(PMCostCode.description);
			_Attributes[_Attributes.Count - 1] = new CostCodeSelectorAttribute() { TaskID = task, AccountID = account, BudgetType = budgetType, AccountGroup = accountGroup, DisableProjectSpecific = disableProjectSpecific };
		}

		public bool IsProjectSpecific(int costCode)
		{
			return ((CostCodeSelectorAttribute)_Attributes[_Attributes.Count - 1]).IsProjectSpecific(costCode);
		}
	}

	public class CostCodeSelectorAttribute : PXCustomSelectorAttribute
	{
        public override void CacheAttached(PXCache sender)
        {
            base.CacheAttached(sender);
            sender.Graph.CommandPreparing.AddHandler(sender.GetItemType(), _FieldName, SubstituteKeyCommandPreparing);
        }

        public string BudgetType { get; set; }
		public Type TaskID { get; set; }
		public Type AccountID { get; set; }
		public Type AccountGroup { get; set; }
		public bool DisableProjectSpecific { get; set; }

		public CostCodeSelectorAttribute() : base(typeof(PMCostCode.costCodeID))
		{
			this.SubstituteKey = typeof(PMCostCode.costCodeCD);
			this.DescriptionField = typeof(PMCostCode.description);
			this.Filterable = true;
			this.FilterEntity = typeof(PMCostCode);
		}

		protected virtual IEnumerable GetRecords()
		{
			PXResultset<PMCostCode> resultset = PXSelect<PMCostCode>.Select(_Graph);
			Dictionary<int, PMCostCode> list = new Dictionary<int, PMCostCode>(resultset.Count);
			foreach (PMCostCode record in resultset)
			{
				record.IsProjectOverride = false;
				if (!list.ContainsKey(record.CostCodeID.Value))
				{
					list.Add(record.CostCodeID.Value, record);
				}
			}

			if (!DisableProjectSpecific)
			{
				foreach (PMBudgetedCostCode budget in GetProjectSpecificRecords().Values)
				{
					PMCostCode found = null;
					if (list.TryGetValue(budget.CostCodeID.Value, out found))
					{
						PMCostCode record = new PMCostCode();
						record.CostCodeID = found.CostCodeID;
						record.CostCodeCD = found.CostCodeCD;
						record.NoteID = found.NoteID;
						record.IsDefault = false;
						record.Description = budget.Description;
						record.IsProjectOverride = true;

						list[budget.CostCodeID.Value] = record;

					}
				}
			}

			return list.Values;
		}

		protected virtual int? GetTaskID()
		{
			if (TaskID == null)
				return null;

			return GetIDByFieldName(TaskID.Name);
		}

		protected virtual int? GetAccountID()
		{
			if (AccountID == null)
				return null;

			return GetIDByFieldName(AccountID.Name);
		}

		protected virtual int? GetAccountGroupID()
		{
			if (AccountGroup == null)
				return null;

			return GetIDByFieldName(AccountGroup.Name);
		}

		protected virtual int? GetIDByFieldName(string fieldName)
		{
			object current = null;
			if (PXView.Currents != null && PXView.Currents.Length > 0)
			{
				current = PXView.Currents[0];
			}
			else
			{
				current = _Graph.Caches[_CacheType].Current;
			}

			return (int?)_Graph.Caches[_CacheType].GetValue(current, fieldName);
		}
				
		protected virtual Dictionary<int?, PMBudgetedCostCode> GetProjectSpecificRecords()
		{
			int? taskID = GetTaskID();

			if (taskID == null)
				return new Dictionary<int?, PMBudgetedCostCode>();

			PXResultset<PMBudgetedCostCode> resultset = null;
			if (AccountID != null)
			{
				int? accountID = GetAccountID();
								
				if (accountID != null)
				{
					var select = new PXSelectJoin<PMBudgetedCostCode,
						InnerJoin<PMAccountGroup, On<PMAccountGroup.groupID, Equal<PMBudgetedCostCode.accountGroupID>>,
						InnerJoin<Account, On<Account.accountGroupID, Equal<PMAccountGroup.groupID>>>>,
						Where<PMBudgetedCostCode.projectTaskID, Equal<Required<PMBudgetedCostCode.projectTaskID>>,
						And<Account.accountID, Equal<Required<Account.accountID>>>>>(_Graph);

					resultset = select.Select(taskID, accountID);
				}
			}
			else if (AccountGroup != null)
			{
				int? accountGroupID = GetAccountGroupID();

				if (accountGroupID != null)
				{
					var select = new PXSelect<PMBudgetedCostCode,
						Where<PMBudgetedCostCode.projectTaskID, Equal<Required<PMBudgetedCostCode.projectTaskID>>,
						And<PMBudgetedCostCode.accountGroupID, Equal<Required<PMBudgetedCostCode.accountGroupID>>>>>(_Graph);

					resultset = select.Select(taskID, accountGroupID);
				}
			}
			

			if (resultset == null)
			{
				if (!string.IsNullOrEmpty(BudgetType))
				{
					var select = new PXSelect<PMBudgetedCostCode, Where<PMBudgetedCostCode.projectTaskID, Equal<Required<PMBudgetedCostCode.projectTaskID>>,
						And<PMBudgetedCostCode.type, Equal<Required<PMBudgetedCostCode.type>>>>>(_Graph);
					resultset = select.Select(taskID, BudgetType);
				}
				else
				{
					var select = new PXSelect<PMBudgetedCostCode, Where<PMBudgetedCostCode.projectTaskID, Equal<Required<PMBudgetedCostCode.projectTaskID>>>>(_Graph);
					resultset = select.Select(taskID);
				}
			}

			Dictionary<int?, PMBudgetedCostCode> records = new Dictionary<int?, PMBudgetedCostCode>(resultset.Count);

			foreach (PMBudgetedCostCode budget in resultset)
			{
				if (!records.ContainsKey(budget.CostCodeID.Value))
				{
					records.Add(budget.CostCodeID.Value, budget);
				}
			}

			return records;
		}

		public bool IsProjectSpecific(int costCode)
		{
			var records = GetProjectSpecificRecords();
			return records.ContainsKey(costCode);
		}
	}
}


