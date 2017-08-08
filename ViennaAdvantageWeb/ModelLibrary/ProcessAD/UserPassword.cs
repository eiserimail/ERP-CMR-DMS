﻿/********************************************************
    * Project Name   : VAdvantage
    * Class Name     : UserPassword
    * Purpose        : Reset Password
    * Class Used     : SvrProcess
    * Chronological    Development
    * Karan            24-May-2011
******************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VAdvantage.Common;
using VAdvantage.DataBase;
using VAdvantage.Process;
using VAdvantage.Classes;
using VAdvantage.Model;
using VAdvantage.Utility;
using VAdvantage.ProcessEngine;


namespace VAdvantage.Process
{
    public class UserPassword : SvrProcess
    {
        private int p_AD_User_ID = -1;
        private String p_OldPassword = null;
        private String p_CurrentPassword = null;
        private String p_NewPassword = null;
        private String p_NewEMail = null;
        private String p_NewEMailUser = null;
        private String p_NewEMailUserPW = null;

        protected override void Prepare()
        {
            ProcessInfoParameter[] para = GetParameter();
            for (int i = 0; i <= para.Length - 1; i++)
            {
                String name = para[i].GetParameterName();
                if (para[i].GetParameter() == null)
                {
                }
                else if (name.Equals("AD_User_ID"))
                    p_AD_User_ID = para[i].GetParameterAsInt();
                else if (name.Equals("OldPassword"))
                    p_OldPassword = para[i].GetParameter().ToString();
                else if (name.Equals("CurrentPassword"))
                    p_CurrentPassword = para[i].GetParameter().ToString();
                else if (name.Equals("NewPassword"))
                    p_NewPassword = para[i].GetParameter().ToString();
                else if (name.Equals("NewEMail"))
                    p_NewEMail = para[i].GetParameter().ToString();
                else if (name.Equals("NewEMailUser"))
                    p_NewEMailUser = para[i].GetParameter().ToString();
                else if (name.Equals("NewEMailUserPW"))
                    p_NewEMailUserPW = para[i].GetParameter().ToString();

            }
        }

        protected override string DoIt()
        {
            if (p_AD_User_ID == -1)
                p_AD_User_ID = GetAD_User_ID();

            MUser user = MUser.Get(GetCtx(), p_AD_User_ID);
            MUser current = MUser.Get(GetCtx(), GetAD_User_ID());


            if (!current.IsAdministrator() && p_AD_User_ID != GetAD_User_ID() && user.HasRole())
                throw new ArgumentException("@UserCannotUpdate@");

            // SuperUser and System passwords can only be updated by themselves
            if (user.IsSystemAdministrator() && p_AD_User_ID != GetAD_User_ID())
                throw new ArgumentException("@UserCannotUpdate@");


            if (string.IsNullOrEmpty(p_CurrentPassword))
            {
                if (string.IsNullOrEmpty(p_OldPassword))
                    throw new ArgumentException("@OldPasswordMandatory@");
                else if (!p_OldPassword.Equals(user.GetPassword()))
                {
                    if (!SecureEngineUtility.SecureEngine.Encrypt(p_OldPassword).Equals(user.GetPassword()))
                    {
                        throw new ArgumentException("@OldPasswordNoMatch@");
                    }
                }
            }

            else if (!p_CurrentPassword.Equals(current.GetPassword()))
                throw new ArgumentException("@OldPasswordNoMatch@");

            String originalPwd = p_NewPassword;

            String sql = "UPDATE AD_User SET Updated=SYSDATE, UpdatedBy=" + GetAD_User_ID();
            if (!string.IsNullOrEmpty(p_NewPassword))
            {
                MColumn column = MColumn.Get(GetCtx(), 417); // Password Column 
                if (column.IsEncrypted())
                    p_NewPassword = SecureEngineUtility.SecureEngine.Encrypt(p_NewPassword);
                sql += ", Password=" + GlobalVariable.TO_STRING(p_NewPassword);
            }
            if (!string.IsNullOrEmpty(p_NewEMail))
                sql += ", Email=" + GlobalVariable.TO_STRING(p_NewEMail);
            if (!string.IsNullOrEmpty(p_NewEMailUser))
                sql += ", EmailUser=" + GlobalVariable.TO_STRING(p_NewEMailUser);
            if (!string.IsNullOrEmpty(p_NewEMailUserPW))
                sql += ", EmailUserPW=" + GlobalVariable.TO_STRING(p_NewEMailUserPW);
            sql += " WHERE AD_User_ID=" + p_AD_User_ID;

            int iRes = DB.ExecuteQuery(sql, null, Get_Trx());
            if (iRes > 0)
            {
                //user.SetPassword(p_NewPassword);
                user.SetPassword(originalPwd);
                return "OK";
            }
            else
                return "@Error@";

        }
    }
}