serach=$1
if [ -z "$serach" ]; then
	echo "needs a parameter, the word/sentence to serch for in source code"
	echo 'EXAMPLE: ./findInSource "HardSync" '
	exit
fi
grep --include=\*.{cs,csproj} -rn './' -e "$serach"