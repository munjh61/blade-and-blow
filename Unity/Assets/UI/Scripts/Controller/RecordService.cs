
using DTO.Auth;
using System.Threading.Tasks;

public class RecordService
{
    public Task<ResponseDTO<GetRecordResp>> GetRecord(Mode mode)
    {
        return Axios.Get<GetRecordResp>($"api/v1/battlestat/{mode}", true);
    }
}