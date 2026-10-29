using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Qscript
{
    public static class DataBase
    {
        public static string MACRO32 = "\r\n; Macroinstructions for defining and calling procedures\r\n\r\nmacro stdcall proc,[arg]\t\t; directly call STDCALL procedure\r\n { common\r\n    if ~ arg eq\r\n   reverse\r\n    pushd arg\r\n   common\r\n    end if\r\n    call proc }\r\n\r\nmacro invoke proc,[arg] \t\t; indirectly call STDCALL procedure\r\n { common\r\n    if ~ arg eq\r\n   reverse\r\n     pushd arg\r\n   common\r\n    end if\r\n    call [proc] }\r\n\r\nmacro ccall proc,[arg]\t\t\t; directly call CDECL procedure\r\n { common\r\n    size@ccall = 0\r\n    if ~ arg eq\r\n   reverse\r\n    pushd arg\r\n    size@ccall = size@ccall+4\r\n   common\r\n    end if\r\n    call proc\r\n    if size@ccall\r\n    add esp,size@ccall\r\n    end if }\r\n\r\nmacro cinvoke proc,[arg]\t\t; indirectly call CDECL procedure\r\n { common\r\n    size@ccall = 0\r\n    if ~ arg eq\r\n   reverse\r\n    pushd arg\r\n    size@ccall = size@ccall+4\r\n   common\r\n    end if\r\n    call [proc]\r\n    if size@ccall\r\n    add esp,size@ccall\r\n    end if }\r\n\r\nmacro proc [args]\t\t\t; define procedure\r\n { common\r\n    match name params, args>\r\n    \\{ define@proc name,<params \\} }\r\n\r\nprologue@proc equ prologuedef\r\n\r\nmacro prologuedef procname,flag,parmbytes,localbytes,reglist\r\n { local loc\r\n   loc = (localbytes+3) and (not 3)\r\n   parmbase@proc equ ebp+8\r\n   localbase@proc equ ebp-loc\r\n   if parmbytes | localbytes\r\n    push ebp\r\n    mov ebp,esp\r\n    if localbytes\r\n     sub esp,loc\r\n    end if\r\n   end if\r\n   irps reg, reglist \\{ push reg \\} }\r\n\r\nepilogue@proc equ epiloguedef\r\n\r\nmacro epiloguedef procname,flag,parmbytes,localbytes,reglist\r\n { irps reg, reglist \\{ reverse pop reg \\}\r\n   if parmbytes | localbytes\r\n    leave\r\n   end if\r\n   if flag and 10000b\r\n    retn\r\n   else\r\n    retn parmbytes\r\n   end if }\r\n\r\nclose@proc equ\r\n\r\nmacro define@proc name,statement\r\n { local params,flag,regs,parmbytes,localbytes,current\r\n   if used name\r\n   name:\r\n   match =stdcall args, statement \\{ params equ args\r\n\t\t\t\t     flag = 11b \\}\r\n   match =stdcall, statement \\{ params equ\r\n\t\t\t\tflag = 11b \\}\r\n   match =c args, statement \\{ params equ args\r\n\t\t\t       flag = 10001b \\}\r\n   match =c, statement \\{ params equ\r\n\t\t\t  flag = 10001b \\}\r\n   match =params, params \\{ params equ statement\r\n\t\t\t    flag = 0 \\}\r\n   match =uses reglist=,args, params \\{ regs equ reglist\r\n\t\t\t\t\tparams equ args \\}\r\n   match =regs =uses reglist, regs params \\{ regs equ reglist\r\n\t\t\t\t\t     params equ \\}\r\n   match =regs, regs \\{ regs equ \\}\r\n   match prologue:reglist, prologue@proc:<regs> \\{ prologue name,flag,parmbytes,localbytes,reglist \\}\r\n   virtual at parmbase@proc\r\n   match =,args, params \\{ defargs@proc args \\}\r\n   match =args@proc args, args@proc params \\{ defargs@proc args \\}\r\n   parmbytes = $-(parmbase@proc)\r\n   end virtual\r\n   name # % = parmbytes/4\r\n   all@vars equ\r\n   current = 0\r\n   macro locals\r\n   \\{ virtual at localbase@proc+current\r\n      macro label def \\\\{ match . type,def> \\\\\\{ deflocal@proc .,label,<type \\\\\\} \\\\}\r\n      struc db [val] \\\\{ \\common deflocal@proc .,db,val \\\\}\r\n      struc du [val] \\\\{ \\common deflocal@proc .,du,val \\\\}\r\n      struc dw [val] \\\\{ \\common deflocal@proc .,dw,val \\\\}\r\n      struc dp [val] \\\\{ \\common deflocal@proc .,dp,val \\\\}\r\n      struc dd [val] \\\\{ \\common deflocal@proc .,dd,val \\\\}\r\n      struc dt [val] \\\\{ \\common deflocal@proc .,dt,val \\\\}\r\n      struc dq [val] \\\\{ \\common deflocal@proc .,dq,val \\\\}\r\n      struc rb cnt \\\\{ deflocal@proc .,rb cnt, \\\\}\r\n      struc rw cnt \\\\{ deflocal@proc .,rw cnt, \\\\}\r\n      struc rp cnt \\\\{ deflocal@proc .,rp cnt, \\\\}\r\n      struc rd cnt \\\\{ deflocal@proc .,rd cnt, \\\\}\r\n      struc rt cnt \\\\{ deflocal@proc .,rt cnt, \\\\}\r\n      struc rq cnt \\\\{ deflocal@proc .,rq cnt, \\\\} \\}\r\n   macro endl\r\n   \\{ purge label\r\n      restruc db,du,dw,dp,dd,dt,dq\r\n      restruc rb,rw,rp,rd,rt,rq\r\n      current = $-(localbase@proc)\r\n      end virtual \\}\r\n   macro ret operand\r\n   \\{ match any, operand \\\\{ retn operand \\\\}\r\n      match , operand \\\\{ match epilogue:reglist, epilogue@proc:<regs> \\\\\\{ epilogue name,flag,parmbytes,localbytes,reglist \\\\\\} \\\\} \\}\r\n   macro finish@proc\r\n   \\{ localbytes = current\r\n      match close:reglist, close@proc:<regs> \\\\{ close name,flag,parmbytes,localbytes,reglist \\\\}\r\n      end if \\} }\r\n\r\nmacro defargs@proc [arg]\r\n { common\r\n    if ~ arg eq\r\n   forward\r\n     local ..arg,current@arg\r\n     match argname:type, arg\r\n      \\{ current@arg equ argname\r\n\t label ..arg type\r\n\t argname equ ..arg\r\n\t if qqword eq type\r\n\t   dd ?,?,?,?,?,?,?,?\r\n\t else if dqword eq type\r\n\t   dd ?,?,?,?\r\n\t else if tbyte eq type\r\n\t   dd ?,?,?\r\n\t else if qword eq type | pword eq type\r\n\t   dd ?,?\r\n\t else\r\n\t   dd ?\r\n\t end if \\}\r\n     match =current@arg,current@arg\r\n      \\{ current@arg equ arg\r\n\t arg equ ..arg\r\n\t ..arg dd ? \\}\r\n   common\r\n     args@proc equ current@arg\r\n   forward\r\n     restore current@arg\r\n   common\r\n    end if }\r\n\r\nmacro deflocal@proc name,def,[val] { name def val }\r\n\r\nmacro deflocal@proc name,def,[val]\r\n { common\r\n    match vars, all@vars \\{ all@vars equ all@vars, \\}\r\n    all@vars equ all@vars name\r\n   forward\r\n    local ..var,..tmp\r\n    ..var def val\r\n    match =?, val \\{ ..tmp equ \\}\r\n    match any =?, val \\{ ..tmp equ \\}\r\n    match any (=?), val \\{ ..tmp equ \\}\r\n    match =label, def \\{ ..tmp equ \\}\r\n    match tmp : value, ..tmp : val\r\n     \\{ tmp: end virtual\r\n\tinitlocal@proc ..var,def value\r\n\tvirtual at tmp\\}\r\n   common\r\n    match first rest, ..var, \\{ name equ first \\} }\r\n\r\nstruc label type { label . type }\r\n\r\nmacro initlocal@proc name,def\r\n { virtual at name\r\n    def\r\n    size@initlocal = $ - name\r\n   end virtual\r\n   position@initlocal = 0\r\n   while size@initlocal > position@initlocal\r\n    virtual at name\r\n     def\r\n     if size@initlocal - position@initlocal < 2\r\n      current@initlocal = 1\r\n      load byte@initlocal byte from name+position@initlocal\r\n     else if size@initlocal - position@initlocal < 4\r\n      current@initlocal = 2\r\n      load word@initlocal word from name+position@initlocal\r\n     else\r\n      current@initlocal = 4\r\n      load dword@initlocal dword from name+position@initlocal\r\n     end if\r\n    end virtual\r\n    if current@initlocal = 1\r\n     mov byte [name+position@initlocal],byte@initlocal\r\n    else if current@initlocal = 2\r\n     mov word [name+position@initlocal],word@initlocal\r\n    else\r\n     mov dword [name+position@initlocal],dword@initlocal\r\n    end if\r\n    position@initlocal = position@initlocal + current@initlocal\r\n   end while }\r\n\r\nmacro endp\r\n { purge ret,locals,endl\r\n   finish@proc\r\n   purge finish@proc\r\n   restore regs@proc\r\n   match all,args@proc \\{ restore all \\}\r\n   restore args@proc\r\n   match all,all@vars \\{ restore all \\} }\r\n\r\n; Macroinstructions for defining data structures\r\n\r\nmacro struct name\r\n { virtual at 0\r\n   define @struct\r\n   field@struct equ name\r\n   match child parent, name \\{ restore field@struct\r\n\t\t\t       field@struct equ child,fields@\\#parent \\}\r\n   sub@struct equ\r\n   struc db [val] \\{ \\common define field@struct .,db,<val> \\}\r\n   struc dw [val] \\{ \\common define field@struct .,dw,<val> \\}\r\n   struc du [val] \\{ \\common define field@struct .,du,<val> \\}\r\n   struc dd [val] \\{ \\common define field@struct .,dd,<val> \\}\r\n   struc dp [val] \\{ \\common define field@struct .,dp,<val> \\}\r\n   struc dq [val] \\{ \\common define field@struct .,dq,<val> \\}\r\n   struc dt [val] \\{ \\common define field@struct .,dt,<val> \\}\r\n   struc rb count \\{ define field@struct .,db,count dup (?) \\}\r\n   struc rw count \\{ define field@struct .,dw,count dup (?) \\}\r\n   struc rd count \\{ define field@struct .,dd,count dup (?) \\}\r\n   struc rp count \\{ define field@struct .,dp,count dup (?) \\}\r\n   struc rq count \\{ define field@struct .,dq,count dup (?) \\}\r\n   struc rt count \\{ define field@struct .,dt,count dup (?) \\}\r\n   macro db [val] \\{ \\common \\local anonymous\r\n\t\t     define field@struct anonymous,db,<val> \\}\r\n   macro dw [val] \\{ \\common \\local anonymous\r\n\t\t     define field@struct anonymous,dw,<val> \\}\r\n   macro du [val] \\{ \\common \\local anonymous\r\n\t\t     define field@struct anonymous,du,<val> \\}\r\n   macro dd [val] \\{ \\common \\local anonymous\r\n\t\t     define field@struct anonymous,dd,<val> \\}\r\n   macro dp [val] \\{ \\common \\local anonymous\r\n\t\t     define field@struct anonymous,dp,<val> \\}\r\n   macro dq [val] \\{ \\common \\local anonymous\r\n\t\t     define field@struct anonymous,dq,<val> \\}\r\n   macro dt [val] \\{ \\common \\local anonymous\r\n\t\t     define field@struct anonymous,dt,<val> \\}\r\n   macro rb count \\{ \\local anonymous\r\n\t\t     define field@struct anonymous,db,count dup (?) \\}\r\n   macro rw count \\{ \\local anonymous\r\n\t\t     define field@struct anonymous,dw,count dup (?) \\}\r\n   macro rd count \\{ \\local anonymous\r\n\t\t     define field@struct anonymous,dd,count dup (?) \\}\r\n   macro rp count \\{ \\local anonymous\r\n\t\t     define field@struct anonymous,dp,count dup (?) \\}\r\n   macro rq count \\{ \\local anonymous\r\n\t\t     define field@struct anonymous,dq,count dup (?) \\}\r\n   macro rt count \\{ \\local anonymous\r\n\t\t     define field@struct anonymous,dt,count dup (?) \\}\r\n   macro union \\{ field@struct equ ,union,<\r\n\t\t  sub@struct equ union \\}\r\n   macro struct \\{ field@struct equ ,substruct,<\r\n\t\t  sub@struct equ substruct \\} }\r\n\r\nmacro ends\r\n { match , sub@struct \\{ restruc db,dw,du,dd,dp,dq,dt\r\n\t\t\t restruc rb,rw,rd,rp,rq,rt\r\n\t\t\t purge db,dw,du,dd,dp,dq,dt\r\n\t\t\t purge rb,rw,rd,rp,rq,rt\r\n\t\t\t purge union,struct\r\n\t\t\t irpv fields,field@struct \\\\{ restore field@struct\r\n\t\t\t\t\t\t      \\\\common define fields@struct fields \\\\}\r\n\t\t\t match name tail,fields@struct, \\\\{ if $\r\n\t\t\t\t\t\t\t    display 'Error: definition of ',\\\\`name,' contains illegal instructions.',0Dh,0Ah\r\n\t\t\t\t\t\t\t    err\r\n\t\t\t\t\t\t\t    end if \\\\}\r\n\t\t\t match name=,fields,fields@struct \\\\{ restore @struct\r\n\t\t\t\t\t\t\t      make@struct name,fields\r\n\t\t\t\t\t\t\t      define fields@\\\\#name fields \\\\}\r\n\t\t\t end virtual \\}\r\n   match any, sub@struct \\{ tmp@struct equ field@struct\r\n\t\t\t    restore field@struct\r\n\t\t\t    field@struct equ tmp@struct> \\}\r\n   restore sub@struct }\r\n\r\nmacro make@struct name,[field,type,def]\r\n { common\r\n    local define\r\n    define equ name\r\n   forward\r\n    local sub\r\n    match , field \\{ make@substruct type,name,sub def\r\n\t\t     define equ define,.,sub, \\}\r\n    match any, field \\{ define equ define,.#field,type,<def> \\}\r\n   common\r\n    match fields, define \\{ define@struct fields \\} }\r\n\r\nmacro define@struct name,[field,type,def]\r\n { common\r\n    virtual\r\n    db `name\r\n    load initial@struct byte from 0\r\n    if initial@struct = '.'\r\n    display 'Error: name of structure should not begin with a dot.',0Dh,0Ah\r\n    err\r\n    end if\r\n    end virtual\r\n    local list\r\n    list equ\r\n   forward\r\n    if ~ field eq .\r\n     name#field type def\r\n     sizeof.#name#field = $ - name#field\r\n    else\r\n     label name#.#type\r\n     rb sizeof.#type\r\n    end if\r\n    local value\r\n    match any, list \\{ list equ list, \\}\r\n    list equ list <value>\r\n   common\r\n    sizeof.#name = $\r\n    restruc name\r\n    match values, list \\{\r\n    struc name value \\\\{ \\\\local \\\\..base\r\n    match , @struct \\\\\\{ define field@struct .,name,<values> \\\\\\}\r\n    match no, @struct \\\\\\{ label \\\\..base\r\n   forward\r\n     match , value \\\\\\\\{ field type def \\\\\\\\}\r\n     match any, value \\\\\\\\{ field type value\r\n\t\t\t    if ~ field eq .\r\n\t\t\t     rb sizeof.#name#field - ($-field)\r\n\t\t\t    end if \\\\\\\\}\r\n   common label . at \\\\..base \\\\\\}\r\n   \\\\}\r\n    macro name value \\\\{\r\n    match , @struct \\\\\\{ \\\\\\local anonymous\r\n\t\t\t\t  define field@struct anonymous,name,<values> \\\\\\}\r\n    match no, @struct \\\\\\{\r\n   forward\r\n     match , value \\\\\\\\{ type def \\\\\\\\}\r\n     match any, value \\\\\\\\{ \\\\\\\\local ..field\r\n\t\t\t   ..field = $\r\n\t\t\t   type value\r\n\t\t\t   if ~ field eq .\r\n\t\t\t    rb sizeof.#name#field - ($-..field)\r\n\t\t\t   end if \\\\\\\\}\r\n   common \\\\\\} \\\\} \\} }\r\n\r\nmacro enable@substruct\r\n { macro make@substruct substruct,parent,name,[field,type,def]\r\n    \\{ \\common\r\n\t\\local define\r\n\tdefine equ parent,name\r\n       \\forward\r\n\t\\local sub\r\n\tmatch , field \\\\{ match any, type \\\\\\{ enable@substruct\r\n\t\t\t\t\t       make@substruct type,parent,sub def\r\n\t\t\t\t\t       purge make@substruct\r\n\t\t\t\t\t       define equ define,.,sub, \\\\\\} \\\\}\r\n\tmatch any, field \\\\{ define equ define,.\\#field,type,<def> \\\\}\r\n       \\common\r\n\tmatch fields, define \\\\{ define@\\#substruct fields \\\\} \\} }\r\n\r\nenable@substruct\r\n\r\nmacro define@union parent,name,[field,type,def]\r\n { common\r\n    virtual at parent#.#name\r\n   forward\r\n    if ~ field eq .\r\n     virtual at parent#.#name\r\n      parent#field type def\r\n      sizeof.#parent#field = $ - parent#field\r\n     end virtual\r\n     if sizeof.#parent#field > $ - parent#.#name\r\n      rb sizeof.#parent#field - ($ - parent#.#name)\r\n     end if\r\n    else\r\n     virtual at parent#.#name\r\n      label parent#.#type\r\n      type def\r\n     end virtual\r\n     label name#.#type at parent#.#name\r\n     if sizeof.#type > $ - parent#.#name\r\n      rb sizeof.#type - ($ - parent#.#name)\r\n     end if\r\n    end if\r\n   common\r\n    sizeof.#name = $ - parent#.#name\r\n    end virtual\r\n    struc name [value] \\{ \\common\r\n    label .\\#name\r\n    last@union equ\r\n   forward\r\n    match any, last@union \\\\{ virtual at .\\#name\r\n\t\t\t       field type def\r\n\t\t\t      end virtual \\\\}\r\n    match , last@union \\\\{ match , value \\\\\\{ field type def \\\\\\}\r\n\t\t\t   match any, value \\\\\\{ field type value \\\\\\} \\\\}\r\n    last@union equ field\r\n   common rb sizeof.#name - ($ - .\\#name) \\}\r\n    macro name [value] \\{ \\common \\local ..anonymous\r\n\t\t\t  ..anonymous name value \\} }\r\n\r\nmacro define@substruct parent,name,[field,type,def]\r\n { common\r\n    virtual at parent#.#name\r\n   forward\r\n    local value\r\n    if ~ field eq .\r\n     parent#field type def\r\n     sizeof.#parent#field = $ - parent#field\r\n    else\r\n     label parent#.#type\r\n     rb sizeof.#type\r\n    end if\r\n   common\r\n    sizeof.#name = $ - parent#.#name\r\n    end virtual\r\n    struc name value \\{\r\n    label .\\#name\r\n   forward\r\n     match , value \\\\{ field type def \\\\}\r\n     match any, value \\\\{ field type value\r\n\t\t\t  if ~ field eq .\r\n\t\t\t   rb sizeof.#parent#field - ($-field)\r\n\t\t\t  end if \\\\}\r\n   common \\}\r\n    macro name value \\{ \\local ..anonymous\r\n\t\t\t..anonymous name \\} }\r\nmacro local [var]\r\n { common\r\n    locals\r\n   forward done@local equ\r\n    match varname[count]:vartype, var\r\n    \\{ match =BYTE, vartype \\\\{ varname rb count\r\n\t\t\t\trestore done@local \\\\}\r\n       match =WORD, vartype \\\\{ varname rw count\r\n\t\t\t\trestore done@local \\\\}\r\n       match =DWORD, vartype \\\\{ varname rd count\r\n\t\t\t\t restore done@local \\\\}\r\n       match =PWORD, vartype \\\\{ varname rp count\r\n\t\t\t\t restore done@local \\\\}\r\n       match =QWORD, vartype \\\\{ varname rq count\r\n\t\t\t\t restore done@local \\\\}\r\n       match =TBYTE, vartype \\\\{ varname rt count\r\n\t\t\t\t restore done@local \\\\}\r\n       match =DQWORD, vartype \\\\{ label varname dqword\r\n\t\t\t\t  rq count*2\r\n\t\t\t\t  restore done@local \\\\}\r\n       match =QQWORD, vartype \\\\{ label varname qqword\r\n\t\t\t\t  rq count*4\r\n\t\t\t\t  restore done@local \\\\}\r\n       match =XWORD, vartype \\\\{ label varname xword\r\n\t\t\t\t rq count*2\r\n\t\t\t\t restore done@local \\\\}\r\n       match =YWORD, vartype \\\\{ label varname yword\r\n\t\t\t\t rq count*4\r\n\t\t\t\t restore done@local \\\\}\r\n       match , done@local \\\\{ virtual\r\n\t\t\t       varname vartype\r\n\t\t\t      end virtual\r\n\t\t\t      rb count*sizeof.\\#vartype\r\n\t\t\t      restore done@local \\\\} \\}\r\n    match :varname:vartype, done@local:var\r\n    \\{ match =BYTE, vartype \\\\{ varname db ?\r\n\t\t\t\trestore done@local \\\\}\r\n       match =WORD, vartype \\\\{ varname dw ?\r\n\t\t\t\trestore done@local \\\\}\r\n       match =DWORD, vartype \\\\{ varname dd ?\r\n\t\t\t\t restore done@local \\\\}\r\n       match =PWORD, vartype \\\\{ varname dp ?\r\n\t\t\t\t restore done@local \\\\}\r\n       match =QWORD, vartype \\\\{ varname dq ?\r\n\t\t\t\t restore done@local \\\\}\r\n       match =TBYTE, vartype \\\\{ varname dt ?\r\n\t\t\t\t restore done@local \\\\}\r\n       match =DQWORD, vartype \\\\{ label varname dqword\r\n\t\t\t\t  dq ?,?\r\n\t\t\t\t  restore done@local \\\\}\r\n       match =QQWORD, vartype \\\\{ label varname qqword\r\n\t\t\t\t  dq ?,?,?,?\r\n\t\t\t\t  restore done@local \\\\}\r\n       match =XWORD, vartype \\\\{ label varname xword\r\n\t\t\t\t dq ?,?\r\n\t\t\t\t restore done@local \\\\}\r\n       match =YWORD, vartype \\\\{ label varname yword\r\n\t\t\t\t dq ?,?,?,?\r\n\t\t\t\t restore done@local \\\\}\r\n       match , done@local \\\\{ varname vartype\r\n\t\t\t      restore done@local \\\\} \\}\r\n    match ,done@local\r\n    \\{ var\r\n       restore done@local \\}\r\n   common\r\n    endl }";
        public static string MACRO64 = "\r\n; Macroinstructions for defining and calling procedures (x64 version)\r\n\r\nmacro invoke proc,[arg]\r\n { common fastcall [proc],arg }\r\n\r\nmacro fastcall proc,[arg]\r\n { common local stackspace,argscount,counter\r\n    if argscount < 4\r\n     stackspace = 4*8\r\n    else if argscount and 1\r\n     stackspace = (argscount+1)*8\r\n    else\r\n     stackspace = argscount*8\r\n    end if\r\n    counter = 0\r\n    if stackspace\r\n     if defined current@frame\r\n      if current@frame<stackspace\r\n       current@frame = stackspace\r\n      end if\r\n     else\r\n      if stackspace\r\n       sub rsp,stackspace\r\n      end if\r\n     end if\r\n    end if\r\n   forward\r\n    counter = counter + 1\r\n    define type@param\r\n    define definition@param arg\r\n    match =float value,definition@param\r\n    \\{ define definition@param value\r\n       define type@param float \\}\r\n    match =addr value,definition@param\r\n    \\{ define definition@param value\r\n       define type@param addr \\}\r\n    match any=,any,definition@param\r\n    \\{ \\local ..string,..continue\r\n       jmp ..continue\r\n       align sizeof.TCHAR\r\n       ..string TCHAR definition@param,0\r\n       ..continue:\r\n       define definition@param ..string\r\n       define type@param addr \\}\r\n    match any,definition@param\r\n    \\{ match \\`any,any\r\n       \\\\{ \\\\local ..string,..continue\r\n\t   jmp ..continue\r\n\t   align sizeof.TCHAR\r\n\t   ..string TCHAR definition@param,0\r\n\t   ..continue:\r\n\t   define definition@param ..string\r\n\t   define type@param addr \\\\} \\}\r\n    match param,definition@param\r\n    \\{ local opcode,origin\r\n       size@param = 0\r\n       if param eqtype 0 | param eqtype 0f | type@param eq addr\r\n\tsize@param = 8\r\n       else if param eqtype byte 0 | param eqtype byte 0f\r\n\tmatch prefix value,definition@param\r\n\t \\\\{ if prefix eq qword\r\n\t      size@param = 8\r\n\t     else if prefix eq dword\r\n\t      size@param = 4\r\n\t     else if prefix eq word\r\n\t      size@param = 2\r\n\t     else if prefix eq byte\r\n\t      size@param = 1\r\n\t     end if \\\\}\r\n       else if ~ param in <xmm0,xmm1,xmm2,xmm3,xmm4,xmm5,xmm6,xmm7,xmm8,xmm9,xmm10,xmm11,xmm12,xmm13,xmm14,xmm15>\r\n\tvirtual\r\n\t origin = $\r\n\t inc param\r\n\t load opcode byte from origin\r\n\t if opcode = 67h | ( opcode > 40h & opcode < 48h )\r\n\t  load opcode byte from origin+1\r\n\t end if\r\n\t if opcode and 0F8h = 48h\r\n\t  size@param = 8\r\n\t else if opcode = 66h\r\n\t  size@param = 2\r\n\t else if opcode = 0FFh\r\n\t  size@param = 4\r\n\t else\r\n\t  size@param = 1\r\n\t end if\r\n\tend virtual\r\n       end if\r\n       if counter = 1\r\n\tif type@param eq float\r\n\t if ~ param eq xmm0\r\n\t  if size@param = 4\r\n\t   if param eqtype byte 0 | param eqtype byte 0f\r\n\t    mov eax,param\r\n\t    movd xmm0,eax\r\n\t   else\r\n\t    movd xmm0,param\r\n\t   end if\r\n\t  else\r\n\t   if param eqtype 0 | param eqtype 0f | param eqtype byte 0 | param eqtype byte 0f\r\n\t    mov rax,param\r\n\t    movq xmm0,rax\r\n\t   else\r\n\t    movq xmm0,param\r\n\t   end if\r\n\t  end if\r\n\t end if\r\n\t if vararg@fastcall & ~ param eq rcx\r\n\t   movq rcx,xmm0\r\n\t end if\r\n\telse if type@param eq addr\r\n\t if ~ param eq rcx\r\n\t  lea rcx,[param]\r\n\t end if\r\n\telse if size@param = 8\r\n\t if ~ param eq rcx\r\n\t  mov rcx,param\r\n\t end if\r\n\telse if size@param = 4\r\n\t if ~ param eq ecx\r\n\t  mov ecx,param\r\n\t end if\r\n\telse if size@param = 2\r\n\t if ~ param eq cx\r\n\t  mov cx,param\r\n\t end if\r\n\telse if size@param = 1\r\n\t if ~ param eq cl\r\n\t  mov cl,param\r\n\t end if\r\n\tend if\r\n       else if counter = 2\r\n\tif type@param eq float\r\n\t if ~ param eq xmm1\r\n\t  if size@param = 4\r\n\t   if param eqtype byte 0 | param eqtype byte 0f\r\n\t    mov eax,param\r\n\t    movd xmm1,eax\r\n\t   else\r\n\t    movd xmm1,param\r\n\t   end if\r\n\t  else\r\n\t   if param eqtype 0 | param eqtype 0f | param eqtype byte 0 | param eqtype byte 0f\r\n\t    mov rax,param\r\n\t    movq xmm1,rax\r\n\t   else\r\n\t    movq xmm1,param\r\n\t   end if\r\n\t  end if\r\n\t end if\r\n\t if vararg@fastcall & ~ param eq rdx\r\n\t   movq rdx,xmm1\r\n\t end if\r\n\telse if type@param eq addr\r\n\t if ~ param eq rdx\r\n\t  lea rdx,[param]\r\n\t end if\r\n\telse if size@param = 8\r\n\t if ~ param eq rdx\r\n\t  mov rdx,param\r\n\t end if\r\n\telse if size@param = 4\r\n\t if ~ param eq edx\r\n\t  mov edx,param\r\n\t end if\r\n\telse if size@param = 2\r\n\t if ~ param eq dx\r\n\t  mov dx,param\r\n\t end if\r\n\telse if size@param = 1\r\n\t if ~ param eq dl\r\n\t  mov dl,param\r\n\t end if\r\n\tend if\r\n       else if counter = 3\r\n\tif type@param eq float\r\n\t if ~ param eq xmm2\r\n\t  if size@param = 4\r\n\t   if param eqtype byte 0 | param eqtype byte 0f\r\n\t    mov eax,param\r\n\t    movd xmm2,eax\r\n\t   else\r\n\t    movd xmm2,param\r\n\t   end if\r\n\t  else\r\n\t   if param eqtype 0 | param eqtype 0f | param eqtype byte 0 | param eqtype byte 0f\r\n\t    mov rax,param\r\n\t    movq xmm2,rax\r\n\t   else\r\n\t    movq xmm2,param\r\n\t   end if\r\n\t  end if\r\n\t end if\r\n\t if vararg@fastcall & ~ param eq r8\r\n\t   movq r8,xmm2\r\n\t end if\r\n\telse if type@param eq addr\r\n\t if ~ param eq r8\r\n\t  lea r8,[param]\r\n\t end if\r\n\telse if size@param = 8\r\n\t if ~ param eq r8\r\n\t  mov r8,param\r\n\t end if\r\n\telse if size@param = 4\r\n\t if ~ param eq r8d\r\n\t  mov r8d,param\r\n\t end if\r\n\telse if size@param = 2\r\n\t if ~ param eq r8w\r\n\t  mov r8w,param\r\n\t end if\r\n\telse if size@param = 1\r\n\t if ~ param eq r8b\r\n\t  mov r8b,param\r\n\t end if\r\n\tend if\r\n       else if counter = 4\r\n\tif type@param eq float\r\n\t if ~ param eq xmm3\r\n\t  if size@param = 4\r\n\t   if param eqtype byte 0 | param eqtype byte 0f\r\n\t    mov eax,param\r\n\t    movd xmm3,eax\r\n\t   else\r\n\t    movd xmm3,param\r\n\t   end if\r\n\t  else\r\n\t   if param eqtype 0 | param eqtype 0f | param eqtype byte 0 | param eqtype byte 0f\r\n\t    mov rax,param\r\n\t    movq xmm3,rax\r\n\t   else\r\n\t    movq xmm3,param\r\n\t   end if\r\n\t  end if\r\n\t end if\r\n\t if vararg@fastcall & ~ param eq r9\r\n\t   movq r9,xmm3\r\n\t end if\r\n\telse if type@param eq addr\r\n\t if ~ param eq r9\r\n\t  lea r9,[param]\r\n\t end if\r\n\telse if size@param = 8\r\n\t if ~ param eq r9\r\n\t  mov r9,param\r\n\t end if\r\n\telse if size@param = 4\r\n\t if ~ param eq r9d\r\n\t  mov r9d,param\r\n\t end if\r\n\telse if size@param = 2\r\n\t if ~ param eq r9w\r\n\t  mov r9w,param\r\n\t end if\r\n\telse if size@param = 1\r\n\t if ~ param eq r9b\r\n\t  mov r9b,param\r\n\t end if\r\n\tend if\r\n       else\r\n\t if type@param eq addr\r\n\t  lea rax,[param]\r\n\t  mov [rsp+(counter-1)*8],rax\r\n\t else if param eqtype [0] | param eqtype byte [0]\r\n\t  if size@param = 8\r\n\t   mov rax,param\r\n\t   mov [rsp+(counter-1)*8],rax\r\n\t  else if size@param = 4\r\n\t   mov eax,param\r\n\t   mov [rsp+(counter-1)*8],eax\r\n\t  else if size@param = 2\r\n\t   mov ax,param\r\n\t   mov [rsp+(counter-1)*8],ax\r\n\t  else\r\n\t   mov al,param\r\n\t   mov [rsp+(counter-1)*8],al\r\n\t  end if\r\n\t else if size@param = 8\r\n\t  virtual\r\n\t   origin = $\r\n\t   mov rax,param\r\n\t   load opcode byte from origin+1\r\n\t  end virtual\r\n\t  if opcode = 0B8h\r\n\t   mov rax,param\r\n\t   mov [rsp+(counter-1)*8],rax\r\n\t  else\r\n\t   mov qword [rsp+(counter-1)*8],param\r\n\t  end if\r\n\t else if param in <xmm0,xmm1,xmm2,xmm3,xmm4,xmm5,xmm6,xmm7,xmm8,xmm9,xmm10,xmm11,xmm12,xmm13,xmm14,xmm15>\r\n\t  movq [rsp+(counter-1)*8],param\r\n\t else\r\n\t  mov [rsp+(counter-1)*8],param\r\n\t end if\r\n       end if \\}\r\n   common\r\n    argscount = counter\r\n    call proc\r\n    if stackspace & ~defined current@frame\r\n     add rsp,stackspace\r\n    end if }\r\n\r\nmacro proc [args]\r\n { common\r\n    match name params, args>\r\n    \\{ define@proc name,<params \\} }\r\n\r\nprologue@proc equ prologuedef\r\n\r\nmacro prologuedef procname,flag,parmbytes,localbytes,reglist\r\n { local loc,fill,counter\r\n   loc = (localbytes+15) and (not 15)\r\n   parmbase@proc equ rbp+16\r\n   localbase@proc equ rbp-loc\r\n   push rbp\r\n   mov rbp,rsp\r\n   if loc+fill\r\n    sub rsp,loc+fill\r\n   end if\r\n   counter = 0\r\n   irps reg, reglist \\{ push reg\r\n\t\t\tcounter = counter+1 \\}\r\n   fill = 8*(counter and 1) }\r\n\r\nepilogue@proc equ epiloguedef\r\n\r\nmacro epiloguedef procname,flag,parmbytes,localbytes,reglist\r\n { irps reg, reglist \\{ reverse pop reg \\}\r\n   leave\r\n   retn }\r\n\r\nclose@proc equ\r\n\r\nmacro define@proc name,statement\r\n { local params,flag,regs,parmbytes,localbytes,current\r\n   if used name\r\n   name:\r\n   match =stdcall args, statement \\{ params equ args\r\n\t\t\t\t     flag = 11b \\}\r\n   match =stdcall, statement \\{ params equ\r\n\t\t\t\tflag = 11b \\}\r\n   match =c args, statement \\{ params equ args\r\n\t\t\t       flag = 10001b \\}\r\n   match =c, statement \\{ params equ\r\n\t\t\t  flag = 10001b \\}\r\n   match =params, params \\{ params equ statement\r\n\t\t\t    flag = 10000b \\}\r\n   match =uses reglist=,args, params \\{ regs equ reglist\r\n\t\t\t\t\tparams equ args \\}\r\n   match =regs =uses reglist, regs params \\{ regs equ reglist\r\n\t\t\t\t\t     params equ \\}\r\n   match =regs, regs \\{ regs equ \\}\r\n   match prologue:reglist, prologue@proc:<regs> \\{ prologue name,flag,parmbytes,localbytes,reglist \\}\r\n   virtual at parmbase@proc\r\n   match =,args, params \\{ defargs@proc args \\}\r\n   match =args@proc args, args@proc params \\{ defargs@proc args \\}\r\n   parmbytes = $-(parmbase@proc)\r\n   end virtual\r\n   name # % = parmbytes/8\r\n   all@vars equ\r\n   current = 0\r\n   macro locals\r\n   \\{ virtual at localbase@proc+current\r\n      macro label def \\\\{ match . type,def> \\\\\\{ deflocal@proc .,label,<type \\\\\\} \\\\}\r\n      struc db [val] \\\\{ \\common deflocal@proc .,db,val \\\\}\r\n      struc du [val] \\\\{ \\common deflocal@proc .,du,val \\\\}\r\n      struc dw [val] \\\\{ \\common deflocal@proc .,dw,val \\\\}\r\n      struc dp [val] \\\\{ \\common deflocal@proc .,dp,val \\\\}\r\n      struc dd [val] \\\\{ \\common deflocal@proc .,dd,val \\\\}\r\n      struc dt [val] \\\\{ \\common deflocal@proc .,dt,val \\\\}\r\n      struc dq [val] \\\\{ \\common deflocal@proc .,dq,val \\\\}\r\n      struc rb cnt \\\\{ deflocal@proc .,rb cnt, \\\\}\r\n      struc rw cnt \\\\{ deflocal@proc .,rw cnt, \\\\}\r\n      struc rp cnt \\\\{ deflocal@proc .,rp cnt, \\\\}\r\n      struc rd cnt \\\\{ deflocal@proc .,rd cnt, \\\\}\r\n      struc rt cnt \\\\{ deflocal@proc .,rt cnt, \\\\}\r\n      struc rq cnt \\\\{ deflocal@proc .,rq cnt, \\\\} \\}\r\n   macro endl\r\n   \\{ purge label\r\n      restruc db,du,dw,dp,dd,dt,dq\r\n      restruc rb,rw,rp,rd,rt,rq\r\n      current = $-(localbase@proc)\r\n      end virtual \\}\r\n   macro ret operand\r\n   \\{ match any, operand \\\\{ retn operand \\\\}\r\n      match , operand \\\\{ match epilogue:reglist, epilogue@proc:<regs> \\\\\\{ epilogue name,flag,parmbytes,localbytes,reglist \\\\\\} \\\\} \\}\r\n   macro finish@proc\r\n   \\{ localbytes = current\r\n      match close:reglist, close@proc:<regs> \\\\{ close name,flag,parmbytes,localbytes,reglist \\\\}\r\n      end if \\} }\r\n\r\nmacro defargs@proc [arg]\r\n { common\r\n    if ~ arg eq\r\n   forward\r\n     local ..arg,current@arg\r\n     match argname:type, arg\r\n      \\{ current@arg equ argname\r\n\t label ..arg type\r\n\t argname equ ..arg\r\n\t if qqword eq type\r\n\t   dq ?,?,?,?\r\n\t else if dqword eq type\r\n\t   dq ?,?\r\n\t else if tbyte eq type\r\n\t   dq ?,?\r\n\t else\r\n\t   dq ?\r\n\t end if \\}\r\n     match =current@arg,current@arg\r\n      \\{ current@arg equ arg\r\n\t arg equ ..arg\r\n\t ..arg dq ? \\}\r\n   common\r\n     args@proc equ current@arg\r\n   forward\r\n     restore current@arg\r\n   common\r\n    end if }\r\n\r\nmacro deflocal@proc name,def,[val] { name def val }\r\n\r\nmacro deflocal@proc name,def,[val]\r\n { common\r\n    match vars, all@vars \\{ all@vars equ all@vars, \\}\r\n    all@vars equ all@vars name\r\n   forward\r\n    local ..var,..tmp\r\n    ..var def val\r\n    match =?, val \\{ ..tmp equ \\}\r\n    match any =?, val \\{ ..tmp equ \\}\r\n    match any (=?), val \\{ ..tmp equ \\}\r\n    match =label, def \\{ ..tmp equ \\}\r\n    match tmp : value, ..tmp : val\r\n     \\{ tmp: end virtual\r\n\tinitlocal@proc ..var,def value\r\n\tvirtual at tmp\\}\r\n   common\r\n    match first rest, ..var, \\{ name equ first \\} }\r\n\r\nstruc label type { label . type }\r\n\r\nmacro initlocal@proc name,def\r\n { virtual at name\r\n    def\r\n    size@initlocal = $ - name\r\n   end virtual\r\n   position@initlocal = 0\r\n   while size@initlocal > position@initlocal\r\n    virtual at name\r\n     def\r\n     if size@initlocal - position@initlocal < 2\r\n      current@initlocal = 1\r\n      load byte@initlocal byte from name+position@initlocal\r\n     else if size@initlocal - position@initlocal < 4\r\n      current@initlocal = 2\r\n      load word@initlocal word from name+position@initlocal\r\n     else if size@initlocal - position@initlocal < 8\r\n      current@initlocal = 4\r\n      load dword@initlocal dword from name+position@initlocal\r\n     else\r\n      load qword@initlocal qword from name+position@initlocal\r\n      if ( qword@initlocal > 0 & qword@initlocal < 80000000h ) | ( qword@initlocal < 0 & qword@initlocal >= -80000000h )\r\n       current@initlocal = 8\r\n      else\r\n       current@initlocal = 4\r\n       dword@initlocal = qword@initlocal and 0FFFFFFFFh\r\n      end if\r\n     end if\r\n    end virtual\r\n    if current@initlocal = 1\r\n     mov byte [name+position@initlocal],byte@initlocal\r\n    else if current@initlocal = 2\r\n     mov word [name+position@initlocal],word@initlocal\r\n    else if current@initlocal = 4\r\n     mov dword [name+position@initlocal],dword@initlocal\r\n    else\r\n     mov qword [name+position@initlocal],qword@initlocal\r\n    end if\r\n    position@initlocal = position@initlocal + current@initlocal\r\n   end while }\r\n\r\nmacro endp\r\n { purge ret,locals,endl\r\n   finish@proc\r\n   purge finish@proc\r\n   restore regs@proc\r\n   match all,args@proc \\{ restore all \\}\r\n   restore args@proc\r\n   match all,all@vars \\{ restore all \\} }\r\n\r\nmacro local [var]\r\n { common\r\n    locals\r\n   forward done@local equ\r\n    match varname[count]:vartype, var\r\n    \\{ match =BYTE, vartype \\\\{ varname rb count\r\n\t\t\t\trestore done@local \\\\}\r\n       match =WORD, vartype \\\\{ varname rw count\r\n\t\t\t\trestore done@local \\\\}\r\n       match =DWORD, vartype \\\\{ varname rd count\r\n\t\t\t\t restore done@local \\\\}\r\n       match =PWORD, vartype \\\\{ varname rp count\r\n\t\t\t\t restore done@local \\\\}\r\n       match =QWORD, vartype \\\\{ varname rq count\r\n\t\t\t\t restore done@local \\\\}\r\n       match =TBYTE, vartype \\\\{ varname rt count\r\n\t\t\t\t restore done@local \\\\}\r\n       match =DQWORD, vartype \\\\{ label varname dqword\r\n\t\t\t\t  rq count*2\r\n\t\t\t\t  restore done@local \\\\}\r\n       match =QQWORD, vartype \\\\{ label varname qqword\r\n\t\t\t\t  rq count*4\r\n\t\t\t\t  restore done@local \\\\}\r\n       match =XWORD, vartype \\\\{ label varname xword\r\n\t\t\t\t rq count*2\r\n\t\t\t\t restore done@local \\\\}\r\n       match =YWORD, vartype \\\\{ label varname yword\r\n\t\t\t\t rq count*4\r\n\t\t\t\t restore done@local \\\\}\r\n       match , done@local \\\\{ virtual\r\n\t\t\t       varname vartype\r\n\t\t\t      end virtual\r\n\t\t\t      rb count*sizeof.\\#vartype\r\n\t\t\t      restore done@local \\\\} \\}\r\n    match :varname:vartype, done@local:var\r\n    \\{ match =BYTE, vartype \\\\{ varname db ?\r\n\t\t\t\trestore done@local \\\\}\r\n       match =WORD, vartype \\\\{ varname dw ?\r\n\t\t\t\trestore done@local \\\\}\r\n       match =DWORD, vartype \\\\{ varname dd ?\r\n\t\t\t\t restore done@local \\\\}\r\n       match =PWORD, vartype \\\\{ varname dp ?\r\n\t\t\t\t restore done@local \\\\}\r\n       match =QWORD, vartype \\\\{ varname dq ?\r\n\t\t\t\t restore done@local \\\\}\r\n       match =TBYTE, vartype \\\\{ varname dt ?\r\n\t\t\t\t restore done@local \\\\}\r\n       match =DQWORD, vartype \\\\{ label varname dqword\r\n\t\t\t\t  dq ?,?\r\n\t\t\t\t  restore done@local \\\\}\r\n       match =QQWORD, vartype \\\\{ label varname qqword\r\n\t\t\t\t  dq ?,?,?,?\r\n\t\t\t\t  restore done@local \\\\}\r\n       match =XWORD, vartype \\\\{ label varname xword\r\n\t\t\t\t dq ?,?\r\n\t\t\t\t restore done@local \\\\}\r\n       match =YWORD, vartype \\\\{ label varname yword\r\n\t\t\t\t dq ?,?,?,?\r\n\t\t\t\t restore done@local \\\\}\r\n       match , done@local \\\\{ varname vartype\r\n\t\t\t      restore done@local \\\\} \\}\r\n    match ,done@local\r\n    \\{ var\r\n       restore done@local \\}\r\n   common\r\n    endl }\r\n\r\nmacro frame\r\n { local size,current\r\n   if size\r\n    sub rsp,size\r\n   end if\r\n   current = 0\r\n   current@frame equ current\r\n   size@frame equ size }\r\n\r\nmacro endf\r\n { size@frame = current@frame\r\n   if size@frame\r\n    add rsp,size@frame\r\n   end if\r\n   restore size@frame,current@frame }\r\n\r\nmacro static_rsp_prologue procname,flag,parmbytes,localbytes,reglist\r\n { local counter,loc,frame,current \r\n   counter = 0 \r\n   irps reg, reglist \\{ push reg \r\n\t\t\tcounter = counter+1 \\} \r\n   loc = (localbytes+7) and (not 7) \r\n   if frame & (counter+loc shr 3+1) and 1 \r\n    loc = loc + 8 \r\n   end if \r\n   framebytes@proc equ frame+loc \r\n   if framebytes@proc \r\n    sub rsp,framebytes@proc \r\n   end if \r\n   localbase@proc equ rsp+frame \r\n   regsbase@proc equ rsp+frame+loc \r\n   parmbase@proc equ rsp+frame+loc+counter*8+8 \r\n   current = 0 \r\n   current@frame equ current \r\n   size@frame equ frame } \r\n\r\nmacro static_rsp_epilogue procname,flag,parmbytes,localbytes,reglist \r\n { if framebytes@proc \r\n    add rsp,framebytes@proc \r\n   end if \r\n   irps reg, reglist \\{ reverse pop reg \\} \r\n   retn } \r\n\r\nmacro static_rsp_close procname,flag,parmbytes,localbytes,reglist \r\n { size@frame = current@frame \r\n   restore size@frame,current@frame }\r\n\r\nmacro stdcall args& { fastcall args }\r\n\r\nmacro cinvoke proc,[arg]\r\n { common ccall [proc],arg }\r\n\r\nmacro ccall proc,[arg]\r\n { common vararg@fastcall = 1\r\n\t  fastcall proc,arg\r\n\t  vararg@fastcall = 0 }\r\n\r\nvararg@fastcall = 0\r\n; Macroinstructions for defining data structures\r\n\r\nmacro struct name\r\n { virtual at 0\r\n   define @struct\r\n   field@struct equ name\r\n   match child parent, name \\{ restore field@struct\r\n\t\t\t       field@struct equ child,fields@\\#parent \\}\r\n   sub@struct equ\r\n   struc db [val] \\{ \\common define field@struct .,db,<val> \\}\r\n   struc dw [val] \\{ \\common define field@struct .,dw,<val> \\}\r\n   struc du [val] \\{ \\common define field@struct .,du,<val> \\}\r\n   struc dd [val] \\{ \\common define field@struct .,dd,<val> \\}\r\n   struc dp [val] \\{ \\common define field@struct .,dp,<val> \\}\r\n   struc dq [val] \\{ \\common define field@struct .,dq,<val> \\}\r\n   struc dt [val] \\{ \\common define field@struct .,dt,<val> \\}\r\n   struc rb count \\{ define field@struct .,db,count dup (?) \\}\r\n   struc rw count \\{ define field@struct .,dw,count dup (?) \\}\r\n   struc rd count \\{ define field@struct .,dd,count dup (?) \\}\r\n   struc rp count \\{ define field@struct .,dp,count dup (?) \\}\r\n   struc rq count \\{ define field@struct .,dq,count dup (?) \\}\r\n   struc rt count \\{ define field@struct .,dt,count dup (?) \\}\r\n   macro db [val] \\{ \\common \\local anonymous\r\n\t\t     define field@struct anonymous,db,<val> \\}\r\n   macro dw [val] \\{ \\common \\local anonymous\r\n\t\t     define field@struct anonymous,dw,<val> \\}\r\n   macro du [val] \\{ \\common \\local anonymous\r\n\t\t     define field@struct anonymous,du,<val> \\}\r\n   macro dd [val] \\{ \\common \\local anonymous\r\n\t\t     define field@struct anonymous,dd,<val> \\}\r\n   macro dp [val] \\{ \\common \\local anonymous\r\n\t\t     define field@struct anonymous,dp,<val> \\}\r\n   macro dq [val] \\{ \\common \\local anonymous\r\n\t\t     define field@struct anonymous,dq,<val> \\}\r\n   macro dt [val] \\{ \\common \\local anonymous\r\n\t\t     define field@struct anonymous,dt,<val> \\}\r\n   macro rb count \\{ \\local anonymous\r\n\t\t     define field@struct anonymous,db,count dup (?) \\}\r\n   macro rw count \\{ \\local anonymous\r\n\t\t     define field@struct anonymous,dw,count dup (?) \\}\r\n   macro rd count \\{ \\local anonymous\r\n\t\t     define field@struct anonymous,dd,count dup (?) \\}\r\n   macro rp count \\{ \\local anonymous\r\n\t\t     define field@struct anonymous,dp,count dup (?) \\}\r\n   macro rq count \\{ \\local anonymous\r\n\t\t     define field@struct anonymous,dq,count dup (?) \\}\r\n   macro rt count \\{ \\local anonymous\r\n\t\t     define field@struct anonymous,dt,count dup (?) \\}\r\n   macro union \\{ field@struct equ ,union,<\r\n\t\t  sub@struct equ union \\}\r\n   macro struct \\{ field@struct equ ,substruct,<\r\n\t\t  sub@struct equ substruct \\} }\r\n\r\nmacro ends\r\n { match , sub@struct \\{ restruc db,dw,du,dd,dp,dq,dt\r\n\t\t\t restruc rb,rw,rd,rp,rq,rt\r\n\t\t\t purge db,dw,du,dd,dp,dq,dt\r\n\t\t\t purge rb,rw,rd,rp,rq,rt\r\n\t\t\t purge union,struct\r\n\t\t\t irpv fields,field@struct \\\\{ restore field@struct\r\n\t\t\t\t\t\t      \\\\common define fields@struct fields \\\\}\r\n\t\t\t match name tail,fields@struct, \\\\{ if $\r\n\t\t\t\t\t\t\t    display 'Error: definition of ',\\\\`name,' contains illegal instructions.',0Dh,0Ah\r\n\t\t\t\t\t\t\t    err\r\n\t\t\t\t\t\t\t    end if \\\\}\r\n\t\t\t match name=,fields,fields@struct \\\\{ restore @struct\r\n\t\t\t\t\t\t\t      make@struct name,fields\r\n\t\t\t\t\t\t\t      define fields@\\\\#name fields \\\\}\r\n\t\t\t end virtual \\}\r\n   match any, sub@struct \\{ tmp@struct equ field@struct\r\n\t\t\t    restore field@struct\r\n\t\t\t    field@struct equ tmp@struct> \\}\r\n   restore sub@struct }\r\n\r\nmacro make@struct name,[field,type,def]\r\n { common\r\n    local define\r\n    define equ name\r\n   forward\r\n    local sub\r\n    match , field \\{ make@substruct type,name,sub def\r\n\t\t     define equ define,.,sub, \\}\r\n    match any, field \\{ define equ define,.#field,type,<def> \\}\r\n   common\r\n    match fields, define \\{ define@struct fields \\} }\r\n\r\nmacro define@struct name,[field,type,def]\r\n { common\r\n    virtual\r\n    db `name\r\n    load initial@struct byte from 0\r\n    if initial@struct = '.'\r\n    display 'Error: name of structure should not begin with a dot.',0Dh,0Ah\r\n    err\r\n    end if\r\n    end virtual\r\n    local list\r\n    list equ\r\n   forward\r\n    if ~ field eq .\r\n     name#field type def\r\n     sizeof.#name#field = $ - name#field\r\n    else\r\n     label name#.#type\r\n     rb sizeof.#type\r\n    end if\r\n    local value\r\n    match any, list \\{ list equ list, \\}\r\n    list equ list <value>\r\n   common\r\n    sizeof.#name = $\r\n    restruc name\r\n    match values, list \\{\r\n    struc name value \\\\{ \\\\local \\\\..base\r\n    match , @struct \\\\\\{ define field@struct .,name,<values> \\\\\\}\r\n    match no, @struct \\\\\\{ label \\\\..base\r\n   forward\r\n     match , value \\\\\\\\{ field type def \\\\\\\\}\r\n     match any, value \\\\\\\\{ field type value\r\n\t\t\t    if ~ field eq .\r\n\t\t\t     rb sizeof.#name#field - ($-field)\r\n\t\t\t    end if \\\\\\\\}\r\n   common label . at \\\\..base \\\\\\}\r\n   \\\\}\r\n    macro name value \\\\{\r\n    match , @struct \\\\\\{ \\\\\\local anonymous\r\n\t\t\t\t  define field@struct anonymous,name,<values> \\\\\\}\r\n    match no, @struct \\\\\\{\r\n   forward\r\n     match , value \\\\\\\\{ type def \\\\\\\\}\r\n     match any, value \\\\\\\\{ \\\\\\\\local ..field\r\n\t\t\t   ..field = $\r\n\t\t\t   type value\r\n\t\t\t   if ~ field eq .\r\n\t\t\t    rb sizeof.#name#field - ($-..field)\r\n\t\t\t   end if \\\\\\\\}\r\n   common \\\\\\} \\\\} \\} }\r\n\r\nmacro enable@substruct\r\n { macro make@substruct substruct,parent,name,[field,type,def]\r\n    \\{ \\common\r\n\t\\local define\r\n\tdefine equ parent,name\r\n       \\forward\r\n\t\\local sub\r\n\tmatch , field \\\\{ match any, type \\\\\\{ enable@substruct\r\n\t\t\t\t\t       make@substruct type,parent,sub def\r\n\t\t\t\t\t       purge make@substruct\r\n\t\t\t\t\t       define equ define,.,sub, \\\\\\} \\\\}\r\n\tmatch any, field \\\\{ define equ define,.\\#field,type,<def> \\\\}\r\n       \\common\r\n\tmatch fields, define \\\\{ define@\\#substruct fields \\\\} \\} }\r\n\r\nenable@substruct\r\n\r\nmacro define@union parent,name,[field,type,def]\r\n { common\r\n    virtual at parent#.#name\r\n   forward\r\n    if ~ field eq .\r\n     virtual at parent#.#name\r\n      parent#field type def\r\n      sizeof.#parent#field = $ - parent#field\r\n     end virtual\r\n     if sizeof.#parent#field > $ - parent#.#name\r\n      rb sizeof.#parent#field - ($ - parent#.#name)\r\n     end if\r\n    else\r\n     virtual at parent#.#name\r\n      label parent#.#type\r\n      type def\r\n     end virtual\r\n     label name#.#type at parent#.#name\r\n     if sizeof.#type > $ - parent#.#name\r\n      rb sizeof.#type - ($ - parent#.#name)\r\n     end if\r\n    end if\r\n   common\r\n    sizeof.#name = $ - parent#.#name\r\n    end virtual\r\n    struc name [value] \\{ \\common\r\n    label .\\#name\r\n    last@union equ\r\n   forward\r\n    match any, last@union \\\\{ virtual at .\\#name\r\n\t\t\t       field type def\r\n\t\t\t      end virtual \\\\}\r\n    match , last@union \\\\{ match , value \\\\\\{ field type def \\\\\\}\r\n\t\t\t   match any, value \\\\\\{ field type value \\\\\\} \\\\}\r\n    last@union equ field\r\n   common rb sizeof.#name - ($ - .\\#name) \\}\r\n    macro name [value] \\{ \\common \\local ..anonymous\r\n\t\t\t  ..anonymous name value \\} }\r\n\r\nmacro define@substruct parent,name,[field,type,def]\r\n { common\r\n    virtual at parent#.#name\r\n   forward\r\n    local value\r\n    if ~ field eq .\r\n     parent#field type def\r\n     sizeof.#parent#field = $ - parent#field\r\n    else\r\n     label parent#.#type\r\n     rb sizeof.#type\r\n    end if\r\n   common\r\n    sizeof.#name = $ - parent#.#name\r\n    end virtual\r\n    struc name value \\{\r\n    label .\\#name\r\n   forward\r\n     match , value \\\\{ field type def \\\\}\r\n     match any, value \\\\{ field type value\r\n\t\t\t  if ~ field eq .\r\n\t\t\t   rb sizeof.#parent#field - ($-field)\r\n\t\t\t  end if \\\\}\r\n   common \\}\r\n    macro name value \\{ \\local ..anonymous\r\n\t\t\t..anonymous name \\} }\r\n";
        public static Dictionary<string, string> types = new Dictionary<string, string>()
        {
            {"int64", "dq"},
            {"int32", "dd"},
            {"int16", "dw"},
            {"int8", "db"},
            {"number", "dd"},
            {"byte", "db"},
            {"string", "du"},
            {"char", "db"},
            {"wchar", "dw"},
            {"float", "dd"},
            {"double", "dq"},
            {"int32_a", "dd"},
            {"bool", "db"},
            {"long", "dd"},
            {"half", "dw"},
            {"function", "dd"},
            {"dq", "dq"},
            {"dd", "dd"},
            {"dw", "dw"},
            {"db", "db"}
        };
        public static Dictionary<string, string> typesarg = new Dictionary<string, string>()
        {
            {"int64", "QWORD"},
            {"int32", "DWORD"},
            {"int16", "WORD"},
            {"int8", "BYTE"},
            {"number", "DWORD"},
            {"byte", "BYTE"},
            {"string", "WORD"},
            {"char", "BYTE"},
            {"wchar", "WORD"},
            {"float", "DWORD"},
            {"double", "QWORD"},
            {"int32_a", "DWORD"},
            {"bool", "BYTE"},
            {"long", "DWORD"},
            {"half", "WORD"},
            {"function", "DWORD"},
            {"dq", "QWORD"},
            {"dd", "DWORD"},
            {"dw", "WORD"},
            {"db", "BYTE"}
        };
        public static Dictionary<string, int> aligns = new Dictionary<string, int>()
        {
            {"int64",   8},
            {"int32",   4},
            {"int16",   2},
            {"int8",    1},
            {"number",  4},
            {"byte",    1},
            {"string",  2},
            {"char",    1},
            {"wchar",   2},
            {"float",   4},
            {"double",  8},
            {"int32_a", 4},
            {"bool",    1},
            {"long",    4},
            {"half",    2},
            {"function",4},
            {"dq",      8},
            {"dd",      4},
            {"dw",      2},
            {"db",      1}
        };
        public static Dictionary<string, string> typesregs = new Dictionary<string, string>()
        {
            {"int64",   "rax"},
            {"int32",   "eax"},
            {"int16",   "ax"},
            {"int8",    "al"},
            {"number",   "eax"},
            {"byte",    "al"},
            {"string",  "ax"},
            {"char",    "al"},
            {"wchar",   "ax"},
            {"float",   "eax"},
            {"double",  "rax"},
            {"int32_a", "eax"},
            {"bool",    "al"},
            {"long",    "eax"},
            {"half",    "ax"},
            {"function", "eax"},
            {"dq",      "rax"},
            {"dd",      "eax"},
            {"dw",      "ax"},
            {"db",      "al"}
        };
        public static Dictionary<string, string[]> regs = new Dictionary<string, string[]>()
        {
            {"eax", new string[] {"rax","eax","ax","al"}},
            {"ecx", new string[] {"rcx","ecx","cx","cl"}},
            {"edx", new string[] {"rdx","edx","dx","dl"}},
            {"ebx", new string[] {"rbx","ebx","bx","bl"}},
            {"edi", new string[] {"rdi","edi","di","ah"}},
            {"esi", new string[] {"rsi","esi","si","ch"}},
            {"esp", new string[] {"rsp","esp","sp","spl"}},
            {"ebp", new string[] {"rbp","ebp","bp","bpl"}},
        };
        public static Dictionary<string, string> uregs = new Dictionary<string, string>()
        {
            {"rax", "eax"},
            {"eax", "eax"},
            {"ax",  "eax"},
            {"al",  "eax"},
            {"ah",  "eax"},
            {"rcx", "ecx"},
            {"ecx", "ecx"},
            {"cx",  "ecx"},
            {"cl",  "ecx"},
            {"ch",  "ecx"},
            {"rdx", "edx"},
            {"edx", "edx"},
            {"dx",  "edx"},
            {"dl",  "edx"},
            {"dh",  "edx"},
            {"rbx", "ebx"},
            {"ebx", "ebx"},
            {"bx",  "ebx"},
            {"bl",  "ebx"},
            {"bh",  "ebx"},
            {"rdi", "edi"},
            {"edi", "edi"},
            {"di",  "edi"},
            {"rsi", "esi"},
            {"esi", "esi"},
            {"si",  "esi"},

            {"rsp", "esp"},
            {"esp", "esp"},
            {"sp",  "esp"},
            {"spl", "esp"},
            {"rbp", "ebp"},
            {"ebp", "ebp"},
            {"bp",  "ebp"},
            {"bpl", "ebp"},
        };
        public static Dictionary<string, int> aregs = new Dictionary<string, int>()
        {
            {"rax", 0},
            {"eax", 1},
            {"ax",  2},
            {"al",  3},
            {"ah",  3},
            {"rcx", 0},
            {"ecx", 1},
            {"cx",  2},
            {"cl",  3},
            {"ch",  3},
            {"rdx", 0},
            {"edx", 1},
            {"dx",  2},
            {"dl",  3},
            {"dh",  3},
            {"rbx", 0},
            {"ebx", 1},
            {"bx",  2},
            {"bl",  3},
            {"bh",  3},
            {"rdi", 0},
            {"edi", 1},
            {"di",  2},
            {"rsi", 0},
            {"esi", 1},
            {"si",  2},
            {"rsp", 0},
            {"esp", 1},
            {"sp",  2},
            {"rbp", 0},
            {"ebp", 1},
            {"bp",  2},
        };
        public static Dictionary<string, int> uaregs = new Dictionary<string, int>()
        {
            {"rax", 8},
            {"eax", 4},
            {"ax",  2},
            {"al",  1},
            {"ah",  1},
            {"rcx", 8},
            {"ecx", 4},
            {"cx",  2},
            {"cl",  1},
            {"ch",  1},
            {"rdx", 8},
            {"edx", 4},
            {"dx",  2},
            {"dl",  1},
            {"dh",  1},
            {"rbx", 8},
            {"ebx", 4},
            {"bx",  2},
            {"bl",  1},
            {"bh",  1},
            {"rdi", 8},
            {"edi", 4},
            {"di",  2},
            {"rsi", 8},
            {"esi", 4},
            {"si",  2},
            {"rsp", 8},
            {"esp", 4},
            {"sp",  2},
            {"rbp", 8},
            {"ebp", 4},
            {"bp",  2},
        };
        public static Dictionary<int, string> sizes = new Dictionary<int, string>()
        {
            {8, "qword"},
            {4, "dword"},
            {2,  "word"},
            {1,  "byte"},
        };
        public static Dictionary<string, string> regschars = new Dictionary<string, string>()
        {
            {"eax", "a"},
            {"ebx", "b"},
            {"edx", "d"},
            {"ecx", "c"}
        };

        public static int isRightOperator (string operation, CommonNode leftType, CommonNode rightType, ref VarSpace varSpace, ref ProgramNode ast)
        {
            int index = 0;
            foreach (var oper in ast.operatorFunctions)
            {
                if (oper.Key.Item1 == operation &&
                    (
                    (oper.Key.Item2._equals(leftType) && oper.Key.Item3._equals(rightType))
                    || (oper.Key.Item2.token.value.Contains(leftType.token.value) && types.ContainsKey(leftType.token.value) && getTypeSize(oper.Key.Item2, ref ast) == getFormulaNodeSize(leftType, ref varSpace, ref ast) && 
                        oper.Key.Item3.token.value.Contains(rightType.token.value) && types.ContainsKey(rightType.token.value) && getTypeSize(oper.Key.Item3, ref ast) == getFormulaNodeSize(rightType, ref varSpace, ref ast)
                        )
                    ))
                {
                    return index;
                }
                index++;
            }
            return -1;
        }
        //public static int isRightSizeOperator(int index, ref ProgramNode ast)
        //{
        //   
        //}
        public static bool isTypeNodeEquels (CommonNode left,  CommonNode right, ref VarSpace varSpace, ref ProgramNode ast)
        {
            if (left._equals(right)) return true;
            (CommonNode leftType, int leftSize) = getFormulaNodeInfo(left, ref varSpace, ref ast);
            (CommonNode rightType, int rightSize) = getFormulaNodeInfo(right, ref varSpace, ref ast);

            if (leftType._equals(rightType)) return true;
            if (leftSize == rightSize) return true;
            return false;
        }

        public static (CommonNode, int) getFormulaNodeInfo(CommonNode node, ref VarSpace varSpace, ref ProgramNode ast)
        {
            int size = 0;
            CommonNode type = node;
            //try
            //{
                switch (node.type)
                {
                    case NT.OFFSET:
                        (type, size) = getFormulaNodeInfo(node.childs[1], ref varSpace, ref ast);
                        break;
                    case NT.LAMBDA:
                        type = new CommonNode(NT.TYPE, new Token(TT.VAR, "function", node.token.pos));
                        size = 4;
                        break;
                    case NT.VAR:
                        type = externGetTypeVar(node, ref varSpace, ref ast);
                        size = getTypeSize(type, ref ast);
                        break;
                    case NT.POSTUNAROPER:
                    case NT.PREUNAROPER:

                    (type, size) = getFormulaNodeInfo(node.childs[0], ref varSpace, ref ast);
                        //type = externGetTypeVar(node.childs[0], ref varSpace, ref ast);
                        //size = getTypeSize(type, ref ast);
                        break;
                    case NT.NUMBER:
                        type = new CommonNode(NT.TYPE, new Token(TT.NUMBER, "int", node.token.pos));
                        size = 4;
                        break;
                    case NT.SIZEOF:
                    case NT.TYPEOF:
                    case NT.ADDRESS:
                        type = new CommonNode(NT.TYPE, new Token(TT.VAR, "int", node.token.pos));
                        size = 4;
                        break;
                    case NT.BINOPER:
                        type = new CommonNode(NT.TYPE, new Token(TT.VAR, "int", node.token.pos));
                        size = 4;

                        (CommonNode _type1, int _size1) = getFormulaNodeInfo(node.childs[0], ref varSpace, ref ast);
                        (CommonNode _type2, int _size2) = getFormulaNodeInfo(node.childs[1], ref varSpace, ref ast);

                        if (_size1 == _size2 || _type1._equals(_type2)) {
                            type = _type1;    
                            size = _size1;
                        }

                        //Console.WriteLine(_type1.token.value + $" {_type1.type} || {_type2.type} " + _type2.token.value);

                        int temp = isRightOperator(node.token.value, _type1, _type2, ref varSpace, ref ast);
                        if (temp > -1) {
                            string OperatorName = ast.operatorFunctions.ElementAt(temp).Value;
                            type = ast.resualtFunc[OperatorName];
                            size = getTypeSize(ast.resualtFunc[OperatorName], ref ast);
                        }

                        break;
                    case NT.FLOAT:
                    case NT.FLOATOPER:
                    case NT.FLOATBINOPER:
                        type = new CommonNode(NT.TYPE, new Token(TT.FLOAT, "float", node.token.pos));
                        size = 4;
                        break;
                    case NT.ASTRING:
                        type = new CommonNode(NT.INDICATOR, new Token(TT.ASTRING, "byte", node.token.pos));
                        size = 1;
                        break;
                    case NT.STRING:
                        type = new CommonNode(NT.INDICATOR, new Token(TT.STRING, "string", node.token.pos));
                        size = 4;
                        break;
                    case NT.CHAR:
                        type = new CommonNode(NT.TYPE, new Token(TT.CHAR, "char", node.token.pos));
                        size = 2;
                        break;
                    case NT.BOOL:
                        type = new CommonNode(NT.TYPE, new Token(TT.NUMBER, "int", node.token.pos));
                        size = 1;
                        break;
                    case NT.TYPEOPER:
                    case NT.TYPE: 
                        node.type = NT.TYPE;
                        if (node.token.value == "void")
                        {
                            type = node;
                            size = 0;
                        }
                        else
                        {
                            type = node;
                            size = getTypeSize(type, ref ast);
                        }
                        break;
                    case NT.CALL:
                        if (varSpace.ContainsKey(node.token.value)) type = varSpace.GetType(node.token.value);
                        else type = ast.resualtFunc[node.token.value];
                        size = getTypeSize(type, ref ast);
                        break;
                    case NT.JMP:
                        type = new CommonNode(NT.TYPE, new Token(TT.NUMBER, "int", node.token.pos));
                        size = 4;
                        break;
                default:
                        type = new CommonNode(NT.TYPE, new Token(TT.NUMBER, "int", node.token.pos));
                        size = 4;
                        break;
                }
            //}
            //catch (Exception e) {
            //    Console.WriteLine($"\n{e.Message}\n{e.Data}\n{e.StackTrace}"); Console.ReadKey();
            //    type = new CommonNode(NT.TYPE, new Token(TT.VAR, "int", node.token.pos));
            //    size = 4;
            //}
            return (type, size);
        }
        public static int getFormulaNodeSize(CommonNode node, ref VarSpace varSpace, ref ProgramNode ast)
        {
            return getFormulaNodeInfo(node, ref varSpace, ref ast).Item2;
        }
        public static CommonNode getFormulaNodeType(CommonNode node, ref VarSpace varSpace, ref ProgramNode ast)
        {
            return getFormulaNodeInfo(node, ref varSpace, ref ast).Item1;
        }
        public static CommonNode getFormulaNodeType(CommonNode node, ref Dictionary<string, CommonNode> vars, ref ProgramNode ast)
        {
            VarSpace varSpace = new VarSpace();
            varSpace.VarsSpaces.Push(vars);
            return getFormulaNodeInfo(node, ref varSpace, ref ast).Item1;
        }
        public static int getStructSize(string type, ref ProgramNode ast)
        {
            int size = 0;
            //Console.WriteLine(type);
            foreach (var _var in ast.structs[type].Values)
            {
                if (_var.type == NT.INDICATOR) size += 4;
                else if (DataBase.types.ContainsKey(_var.token.value))
                {
                    string classsize = DataBase.types[_var.token.value];
                    if (_var.token.value == "dq") size += 8;
                    else if (_var.token.value == "dd") size += 4;
                    else if (_var.token.value == "dw") size += 2;
                    else if (_var.token.value == "db") size += 1;
                }
                else if (_var.token.value == "dq") size += 8;
                else if (_var.token.value == "dd") size += 4;
                else if (_var.token.value == "dw") size += 2;
                else if (_var.token.value == "db") size += 1;
                else size += getStructSize(_var.token.value, ref ast);
            }

            return size;
        }
        public static int getTypeSize(CommonNode type, ref ProgramNode ast)
        {
            if (type.type == NT.NULL) return 4;
            if (type.type == NT.INDICATOR) return 4;
            if (aligns.ContainsKey(type.token.value)) return aligns[type.token.value];
            else return getStructSize(type.token.value, ref ast);
        }
        public static CommonNode externGetTypeVar(CommonNode var, ref VarSpace varSpace, ref ProgramNode ast)
        {
            CommonNode type;
            string name = var.token.value;

            if (name.Contains(".") || name.Contains(","))
            {
                int n = 0;
                string[] strs = name.Split('.', ',');
                string strct = varSpace.GetType(strs[0]).token.value;
            start:
                if (
                    !ast.structs.ContainsKey(strct) ||
                    !ast.structs[strct].ContainsKey(strs[n + 1])
                    )
                {
                    Syntax.SyntaxError($"Ошибка Поле:{strs[n + 1]} не существует в {strct}", var);
                }
                type = ast.structs[strct][strs[n + 1]];
                strct = type.token.value;

                if (n + 2 != strs.Length) { n++; goto start; } // strct = ProgramAst.structs[strct][strs[n]].token.value; 
            }
            else type = varSpace.GetType(name);
            return type;
        }

        public static void PrintAllStructs (ref ProgramNode ast)
        {
            for (int i = 0; i < ast.structs.Count; i++)
            {
                Console.WriteLine($"[{ast.structs.ElementAt(i).Key}] - ");
                foreach (var child in ast.structs.ElementAt(i).Value)
                {
                    Console.WriteLine($"  [{child.Key}] = {child.Value.token.value}, {child.Value.type}");
                }
            }
        }
    }
}
