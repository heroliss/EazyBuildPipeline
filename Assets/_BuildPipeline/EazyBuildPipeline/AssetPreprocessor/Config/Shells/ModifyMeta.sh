#!/bin/bash
function Log()
{
    echo -e $* | awk -F ' \t ' '{printf ("%-10s \t %-21s \t %s\n", $1, $2, $3)}' >> ${logFilePath} #此处用“ \t ”做分隔符，每列宽度依次应为4的倍数-2,-3,-0,-1...
}

if (( $# < 6 ));then echo "Need at least 6 Parameters"; exit 0;fi
if [ ! -d $1 ];then mkdir -p $1;fi
#logFilePath="$1/[$(date '+%y-%m-%d_%H.%M.%S')]ModifyMeta.txt"
logFilePath=$1
rootPath=$2
platform=$3
startIndex=4

#OLD_IFS=$IFS
IFS=$'\n'
files=(`find ${rootPath} -name '*.meta' -type f`)
#IFS=$OLD_IFS
filesNum=${#files[@]}
count=0
successCount=0
skipCount=0

#开始信息
echo "LogFilePath: ${logFilePath}"
echo "Total: ${filesNum}"
Log "Shell: $0"
Log "Parameters: $*"
Log "Start at $(date '+%Y-%m-%d %H:%M:%S')\n"
Log "State \t Message \t Target File Path"
Log "---------- \t --------------------- \t --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------"

for file in ${files[@]}
do
    echo "Applying: $((count++)) ${file}"

    file_content=$(cat ${file} 2>&1)
    file_content_origin=$file_content
    returnValue=$?
    if [ $returnValue != 0 ];then
        #错误信息
        errorMessage="Error(${returnValue}) \t ${file_content} (Operation: cat) \t ${file}"
        echo -e "Error: ${errorMessage}"
        Log "${errorMessage}"
        #统计信息
        echo "Skip: ${skipCount}"
        echo "Success: ${successCount}"
        doneMessage="\nDone at $(date '+%Y-%m-%d %H:%M:%S') \nTotal: ${filesNum}\nSkip: ${skipCount}\nSuccess: ${successCount}"
        Log "${doneMessage}"

        exit 1
    fi

    for((i=${startIndex};i<=$#;))
    do
        eval b=\${$((i++))}
        option=$b
        eval b=\${$((i++))}
        origin=$b
        eval b=\${$((i++))}
        target=$b

        operation="/buildTarget: ${platform}/,/buildTarget/s/${option}: ${origin}.*$/${option}: ${target}/"

        file_content=$( echo "${file_content}" | sed -e ${operation} 2>&1 )
        returnValue=$?
        if [ $returnValue != 0 ];then
            #错误信息
            errorMessage="Error(${returnValue}) \t ${file_content} (Operation: sed -e ${operation}) \t ${file} "
            echo -e "Error: ${errorMessage}"
            Log "${errorMessage}"
            #统计信息
            echo "Skip: ${skipCount}"
            echo "Success: ${successCount}"
            doneMessage="\nDone at $(date '+%Y-%m-%d %H:%M:%S') \nTotal: ${filesNum}\nSkip: ${skipCount}\nSuccess: ${successCount}"
            Log "${doneMessage}"

            exit 2
        fi
    done
    if [ "${file_content}" == "${file_content_origin}" ];then
        ((skipCount++))
        Log "Skip \t No Change. \t ${file}"
    else
        echo "${file_content}" > "${file}"
        returnValue=$?
        if [ $returnValue != 0 ];then
            #错误信息
            errorMessage="Error(${returnValue}) \t Can Not Write File. \t ${file}"
            echo -e "Error: ${errorMessage}"
            Log "${errorMessage}"
            #统计信息
            echo "Skip: ${skipCount}"
            echo "Success: ${successCount}"
            doneMessage="\nDone at $(date '+%Y-%m-%d %H:%M:%S') \nTotal: ${filesNum}\nSkip: ${skipCount}\nSuccess: ${successCount}"
            Log "${doneMessage}"

            exit 3
        fi
        Log "Success \t  \t ${file}"
        ((successCount++))
    fi
done
#统计信息
echo "Skip: ${skipCount}"
echo "Success: ${successCount}"
doneMessage="\nDone at $(date '+%Y-%m-%d %H:%M:%S') \nTotal: ${filesNum}\nSkip: ${skipCount}\nSuccess: ${successCount}"
Log "${doneMessage}"