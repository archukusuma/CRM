using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CheckUserForTransaction
{
    public class CheckUserForTran : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = factory.CreateOrganizationService(context.UserId);

            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            if (context.Depth > 1)
                return;

            var initiatingUsr = Guid.Empty;
            var usrBuId = Guid.Empty;
            var userGuid = Guid.Empty;
            var offGuid = Guid.Empty;
            var owningBU = Guid.Empty;
            var userName = (string)null;
            var exceptionStr = (string)null;
            var userBUlist = new List<Guid>();

            try
            {
                if (context.PrimaryEntityName == "mdoc_transactionrequest" && context.MessageName == "Create")
                {
                    if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
                    {
                        var tranReqEntity = (Entity)context.InputParameters["Target"];
                        var trancReqGuid = tranReqEntity.Id;
                        var trancReqLN = tranReqEntity.LogicalName;

                        var tranReqEnt = service.Retrieve(trancReqLN, trancReqGuid, new ColumnSet("mdoc_userid", "mdoc_offenderid"));

                        if (tranReqEnt.Contains("mdoc_userid"))
                        {
                            var userGuidStr = (string)tranReqEnt["mdoc_userid"];
                            userGuid = new Guid(userGuidStr);
                        }
                        else
                        {
                            exceptionStr = "mdoc_transactionrequest: Cannot Get User Guid.";
                            throw new InvalidPluginExecutionException("CheckUserForTransaction Plugin Error -> mdoc_transactionrequest: Cannot Get User Guid.___");
                        }

                        if (tranReqEnt.Contains("mdoc_offenderid"))
                        {
                            var offGuidStr = (string)tranReqEnt["mdoc_offenderid"];
                            offGuid = new Guid(offGuidStr);
                            var offEnt = service.Retrieve("mdoc_offender", offGuid, new ColumnSet("owningbusinessunit"));

                            if (offEnt.Contains("owningbusinessunit"))
                            {
                                owningBU = ((EntityReference)offEnt["owningbusinessunit"]).Id;
                            }
                            else
                            {
                                exceptionStr = "mdoc_transactionrequest -> mdoc_offender: Cannot Get OwningBusinessUnit Guid.";
                                throw new InvalidPluginExecutionException("CheckUserForTransaction Plugin Error -> mdoc_transactionrequest -> mdoc_offender: OwningBusinessUnit is null for Offender Guid " + "\'" + offGuid + "\'.___");
                            }
                        }
                        else
                        {
                            exceptionStr = "mdoc_transactionrequest: Cannot Get Offender Guid.";
                            throw new InvalidPluginExecutionException("CheckUserForTransaction Plugin Error -> mdoc_transactionrequest: Cannot Get Offender Guid.___");
                        }
                    }
                    else
                    {
                        exceptionStr = "mdoc_transactionrequest: InputParameters does NOT Contain Target";
                        throw new InvalidPluginExecutionException("CheckUserForTransaction Plugin Error -> mdoc_transactionrequest: InputParameters does NOT Contain \'Target\' or \'Target\' is NOT Entity!.___");
                    }
                }

                if (context.PrimaryEntityName == "mdoc_transaction")
                {
                    var trancGuid = Guid.Empty;
                    var trancLN = (string)null;

                    if (context.MessageName == "Create" || context.MessageName == "Update" || context.MessageName == "Delete")
                    {
                        if (context.InputParameters.Contains("Target") && (context.InputParameters["Target"] is Entity || context.InputParameters["Target"] is EntityReference))
                        {
                            if (context.MessageName == "Create" || context.MessageName == "Update")
                            {
                                var tranEntity = (Entity)context.InputParameters["Target"];
                                trancGuid = tranEntity.Id;
                                trancLN = tranEntity.LogicalName;
                            }
                            else if (context.MessageName == "Delete")
                            {
                                var tranEntityRef = (EntityReference)context.InputParameters["Target"];
                                trancGuid = tranEntityRef.Id;
                                trancLN = tranEntityRef.LogicalName;
                            }
                        }
                        else
                        {
                            exceptionStr = "mdoc_transaction: InputParameters does NOT Contain Target";
                            throw new InvalidPluginExecutionException("CheckUserForTransaction Plugin Error -> mdoc_transaction: InputParameters does NOT Contain \'Target\' or \'Target\' is NOT Entity Or EntityReference!.");
                        }
                    }

                    if (context.PrimaryEntityName == "mdoc_transaction" && context.MessageName == "SetStateDynamicEntity")
                    {
                        if (context.InputParameters.Contains("EntityMoniker") && context.InputParameters["EntityMoniker"] is EntityReference)
                        {
                            var tranEntityRef = (EntityReference)context.InputParameters["EntityMoniker"];
                            trancGuid = tranEntityRef.Id;
                            trancLN = tranEntityRef.LogicalName;
                        }
                        else
                        {
                            exceptionStr = "mdoc_transaction: InputParameters does NOT Contain EntityMoniker";
                            throw new InvalidPluginExecutionException("CheckUserForTransaction Plugin Error -> mdoc_transaction: InputParameters does NOT contain EntityMoniker OR EntityMoniker is NOT an EntityReference!.");
                        }
                    }

                    var tranEnt = service.Retrieve(trancLN, trancGuid, new ColumnSet("mdoc_offenderid"));

                    if (tranEnt.Contains("mdoc_offenderid"))
                    {
                        offGuid = ((EntityReference)tranEnt["mdoc_offenderid"]).Id;
                        var offEnt = service.Retrieve("mdoc_offender", offGuid, new ColumnSet("owningbusinessunit"));

                        if (offEnt.Contains("owningbusinessunit"))
                        {
                            owningBU = ((EntityReference)offEnt["owningbusinessunit"]).Id;
                        }
                        else
                        {
                            exceptionStr = "mdoc_transaction -> mdoc_offender: Cannot Get OwningBusinessUnit Guid.";
                            throw new InvalidPluginExecutionException("CheckUserForTransaction Plugin Error -> mdoc_transaction -> mdoc_offender: OwningBusinessUnit is null for Offender Guid " + "\'" + offGuid + "\'");
                        }
                    }
                    else
                    {
                        exceptionStr = "mdoc_transaction: Cannot Get Offender Guid.";
                        throw new InvalidPluginExecutionException("CheckUserForTransaction Plugin Error -> mdoc_transaction: Cannot Get Offender Guid.");
                    }

                    userGuid = context.InitiatingUserId;
                }

                #region CheckUser
                using (var serviceContext = new OrganizationServiceContext(service))
                {

                    var userQ = from u in serviceContext.CreateQuery("systemuser")
                                where u["systemuserid"].Equals(userGuid)
                                select new { usrName = u["fullname"], buId = u["businessunitid"] };

                    if (userQ.ToList().Count > 0)
                    {
                        userName = (string)userQ.First().usrName;
                        usrBuId = ((EntityReference)userQ.First().buId).Id;

                        if (!userBUlist.Contains(usrBuId))
                            userBUlist.Add(usrBuId);
                    }
                    else
                    {
                        exceptionStr = "Cannot Get User Id";
                        if (context.PrimaryEntityName == "mdoc_transactionrequest")
                            throw new InvalidPluginExecutionException("CheckUserForTransaction Plugin Error: Cannot Get User Id: " + userGuid + " Information.___");
                        else
                            throw new InvalidPluginExecutionException("CheckUserForTransaction Plugin Error: Cannot Get User Id: " + userGuid + " Information.");
                    }
                }

                var exp1 = new Regex("SA_DOC(.+?)SSIS(.+?)");
                var results1 = exp1.IsMatch(userName);
                if (results1)
                    return;

                var exp2 = new Regex(@"SA_DOC(.+?)CRM(.+?)");
                var results2 = exp2.IsMatch(userName);
                if (results2)
                    return;

                using (var serviceContext = new OrganizationServiceContext(service))
                {
                    var tcUpdSecRoleId = Guid.Empty;
                    var tcUpdSecRoleName = (string)null;

                    var tranUpdateSecRoleQ = from r in serviceContext.CreateQuery("role")
                                             where r["name"].Equals("Timecomp - Update") && r["businessunitid"].Equals(usrBuId)
                                             select new { roleId = r["roleid"], roleName = r["name"] };

                    if (tranUpdateSecRoleQ.ToList().Count > 0)
                    {
                        tcUpdSecRoleId = (Guid)tranUpdateSecRoleQ.FirstOrDefault().roleId;
                        tcUpdSecRoleName = (string)tranUpdateSecRoleQ.FirstOrDefault().roleName;
                    }
                    else
                    {
                        exceptionStr = "\'Timecomp - Update\' Security Role Does NOT Exist in OMS";

                        if (context.PrimaryEntityName == "mdoc_transactionrequest")
                            throw new InvalidPluginExecutionException("CheckUserForTransaction Plugin Error: \'System Administrator\' Security Role Does NOT Exist in OMS.___");
                        else
                            throw new InvalidPluginExecutionException("CheckUserForTransaction Plugin Error: \'System Administrator\' Security Role Does NOT Exist in OMS.");

                    }

                    var userUpdateRoleQ = from ur in serviceContext.CreateQuery("systemuserroles")
                                          where ur["systemuserid"].Equals(userGuid) && ur["roleid"].Equals(tcUpdSecRoleId)
                                          select ur["systemuserid"];

                    if (userUpdateRoleQ.ToList().Count > 0)
                        return;
                }

                using (var serviceContext = new OrganizationServiceContext(service))
                {
                    var sysAdminSecRoleId = Guid.Empty;
                    var sysAdminSecRoleName = (string)null;

                    var tranUpdateSecRoleQ = from r in serviceContext.CreateQuery("role")
                                             where r["name"].Equals("System Administrator") && r["businessunitid"].Equals(usrBuId)
                                             select new { roleId = r["roleid"], roleName = r["name"] };

                    if (tranUpdateSecRoleQ.ToList().Count > 0)
                    {
                        sysAdminSecRoleId = (Guid)tranUpdateSecRoleQ.FirstOrDefault().roleId;
                        sysAdminSecRoleName = (string)tranUpdateSecRoleQ.FirstOrDefault().roleName;
                    }
                    else
                    {
                        exceptionStr = "\'System Administrator\' Security Role Does NOT Exist in OMS";

                        if (context.PrimaryEntityName == "mdoc_transactionrequest")
                            throw new InvalidPluginExecutionException("CheckUserForTransaction Plugin Error: \'System Administrator\' Security Role Does NOT Exist in OMS.___");
                        else
                            throw new InvalidPluginExecutionException("CheckUserForTransaction Plugin Error: \'System Administrator\' Security Role Does NOT Exist in OMS.");

                    }

                    var userUpdateRoleQ = from ur in serviceContext.CreateQuery("systemuserroles")
                                          where ur["systemuserid"].Equals(userGuid) && ur["roleid"].Equals(sysAdminSecRoleId)
                                          select ur["systemuserid"];

                    if (userUpdateRoleQ.ToList().Count > 0)
                        return;
                }

                using (var serviceContext = new OrganizationServiceContext(service))
                {

                    var userQ = from r in serviceContext.CreateQuery("mdoc_recordstransactionaccess")
                                where r["mdoc_userid"].Equals(userGuid)
                                select new { buId = r["mdoc_businessunitid"] };

                    foreach (var bu in userQ)
                    {
                        usrBuId = ((EntityReference)bu.buId).Id;
                        if (!userBUlist.Contains(usrBuId))
                            userBUlist.Add(usrBuId);
                    }
                }

                if (!userBUlist.Contains(owningBU))
                {
                    exceptionStr = "Cannot Process Request";

                    if (context.PrimaryEntityName == "mdoc_transactionrequest")
                        throw new InvalidPluginExecutionException("Sorry " + "\'" + userName + "\'" + " You Do Not Have The Required \'" + context.MessageName + "\' Privilege!.___");
                    else
                        throw new InvalidPluginExecutionException("Sorry " + "\'" + userName + "\'" + " You Do Not Have The Required \'" + context.MessageName + "\' Privilege!.");
                }
                #endregion
            }
            catch (Exception ex)
            {
                if (exceptionStr == "mdoc_transactionrequest: InputParameters does NOT Contain Target")
                    throw new InvalidPluginExecutionException("CheckUserForTransaction Plugin Error -> mdoc_transactionrequest: InputParameters does NOT Contain \'Target\' or \'Target\' is NOT Entity!.___");
                else if (exceptionStr == "mdoc_transactionrequest: Cannot Get User Guid.")
                    throw new InvalidPluginExecutionException("CheckUserForTransaction Plugin Error -> mdoc_transactionrequest: Cannot Get User Guid.___");
                else if (exceptionStr == "mdoc_transactionrequest: Cannot Get Offender Guid.")
                    throw new InvalidPluginExecutionException("CheckUserForTransaction Plugin Error -> mdoc_transactionrequest: Cannot Get Offender Guid.___");
                else if (exceptionStr == "mdoc_transactionrequest -> mdoc_offender: Cannot Get OwningBusinessUnit Guid.")
                    throw new InvalidPluginExecutionException("CheckUserForTransaction Plugin Error -> mdoc_transactionrequest -> mdoc_offender: OwningBusinessUnit is null for Offender Guid " + "\'" + offGuid + "\'.___");
                else if (exceptionStr == "mdoc_transaction: InputParameters does NOT Contain Target")
                    throw new InvalidPluginExecutionException("CheckUserForTransaction Plugin Error -> mdoc_transaction: InputParameters does NOT Contain \'Target\' or \'Target\' is NOT Entity Or EntityReference!.");
                else if (exceptionStr == "mdoc_transaction: InputParameters does NOT Contain EntityMoniker")
                    throw new InvalidPluginExecutionException("CheckUserForTransaction Plugin Error -> InputParameters does NOT contain EntityMoniker OR EntityMoniker is NOT an EntityReference");
                else if (exceptionStr == "mdoc_transaction: InputParameters does NOT Contain Target")
                    throw new InvalidPluginExecutionException("CheckUserForTransaction Plugin Error -> mdoc_transaction: InputParameters does NOT Contain \'Target\' or \'Target\' is NOT Entity!.");
                else if (exceptionStr == "mdoc_transaction: Cannot Get Offender Guid.")
                    throw new InvalidPluginExecutionException("CheckUserForTransaction Plugin Error -> mdoc_transaction: Cannot Get Offender Guid.");
                else if (exceptionStr == "mdoc_transaction -> mdoc_offender: Cannot Get OwningBusinessUnit Guid.")
                    throw new InvalidPluginExecutionException("CheckUserForTransaction Plugin Error -> mdoc_transaction -> mdoc_offender: OwningBusinessUnit is null for Offender Guid " + "\'" + offGuid + "\'");
                else if (exceptionStr == "Cannot Get User Id")
                {
                    if (context.PrimaryEntityName == "mdoc_transactionrequest")
                        throw new InvalidPluginExecutionException("CheckUserForTransaction Plugin Error: Cannot Get User Id: " + userGuid + " Information.___");
                    else
                        throw new InvalidPluginExecutionException("CheckUserForTransaction Plugin Error: Cannot Get User Id: " + userGuid + " Information.");
                }
                else if (exceptionStr == "Cannot Process Request")
                {
                    if (context.PrimaryEntityName == "mdoc_transactionrequest")
                        throw new InvalidPluginExecutionException("Sorry " + "\'" + userName + "\'" + " You Do Not Have The Required \'" + context.MessageName + "\' Privilege!.___");
                    else
                        throw new InvalidPluginExecutionException("Sorry " + "\'" + userName + "\'" + " You Do Not Have The Required \'" + context.MessageName + "\' Privilege!.");
                }
                else if (exceptionStr == "System Administrator Security Role Does NOT Exist in OMS")
                {
                    if (context.PrimaryEntityName == "mdoc_transactionrequest")
                        throw new InvalidPluginExecutionException("CheckUserForTransaction Plugin Error: \'System Administrator\' Security Role Does NOT Exist in OMS.___");
                    else
                        throw new InvalidPluginExecutionException("CheckUserForTransaction Plugin Error: \'System Administrator\' Security Role Does NOT Exist in OMS.");
                }
                else throw new InvalidPluginExecutionException("CheckUserForTransaction Plugin Error: " + ex.Message);
            }
        }
    }
}
