


signed ggt(int  x, int y) {
    
    if(y!=0) {
        while(x!=y) {
            if(x>y) {
                x = x - y;
                
            } else {
                y = y - x;   
            } 
           
        }   
    }
    return x; 
}

int main (){
    ggt(6,9);
}
